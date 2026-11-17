using Content.Server.Administration;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Power.WirelessPower;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._TP.Power.WirelessPower;

/// <summary>
///     Moves charge from wireless power transmitters to every receiver on the same channel.
///     Transmitters charge from an input node and receivers discharge onto the output mode,
///         so both ends only draw / supply what the power networks actually want.
/// </summary>
public sealed partial class WirelessPowerSystem : EntitySystem
{
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    private static readonly ProtoId<ToolQualityPrototype> PulsingQuality = "Pulsing";

    /// <summary>
    ///     Seconds between transfers. Device batteries need to buffer at least this long at full rate.
    /// </summary>
    private const float UpdateInterval = 1f;

    private float _accumulator;

    private readonly Dictionary<int, ChannelGroup> _channels = new();

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<WirelessPowerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.RandomizeChannel)
            ent.Comp.Channel = _random.Next(1, ent.Comp.MaxChannel + 1);
    }

    #region Channel configuration
    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<WirelessPowerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, PulsingQuality))
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Handled = true;

        var user = args.User;
        var target = ent.Owner;

        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("wireless-power-dialog-title"),
            Loc.GetString("wireless-power-dialog-prompt", ("max", ent.Comp.MaxChannel)),
            (int channel) =>
            {
                if (TerminatingOrDeleted(target)
                    || !TryComp<WirelessPowerComponent>(target, out var comp)
                    || !_interaction.InRangeUnobstructed(user, target))
                    return;

                if (!SetChannel((target, comp), channel))
                {
                    _popup.PopupEntity(Loc.GetString("wireless-power-channel-invalid", ("max", comp.MaxChannel)), target, user);
                    return;
                }

                _popup.PopupEntity(Loc.GetString("wireless-power-channel-set", ("channel", channel)), target, user);
            });
    }

    /// <summary>
    ///     Tunes a device to a channel. Takes effect on the next transfer.
    /// </summary>
    /// <returns>false if the channel is out of range</returns>
    public bool SetChannel(Entity<WirelessPowerComponent?> ent, int channel)
    {
        if (!Resolve(ent, ref ent.Comp) || channel < 0 || channel > ent.Comp.MaxChannel)
            return false;

        ent.Comp.Channel = channel;
        return true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<WirelessPowerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var comp = ent.Comp;
        using (args.PushGroup(nameof(WirelessPowerComponent)))
        {
            args.PushMarkup(Loc.GetString("wireless-power-examine-channel", ("channel", comp.Channel)));

            if (!Transform(ent).Anchored)
            {
                args.PushMarkup(Loc.GetString("wireless-power-examine-unanchored"));
                return;
            }

            if (comp.LinkedCount == 0)
            {
                args.PushMarkup(Loc.GetString("wireless-power-examine-no-link"));
                return;
            }

            if (comp.Mode == WirelessPowerMode.Transmitter)
            {
                args.PushMarkup(Loc.GetString("wireless-power-examine-transmitting",
                    ("count", comp.LinkedCount),
                    ("power", comp.LastTransfer),
                    ("efficiency", (int) MathF.Round(comp.LastEfficiency * 100f))));
            }
            else
            {
                args.PushMarkup(Loc.GetString("wireless-power-examine-receiving",
                    ("count", comp.LinkedCount),
                    ("power", comp.LastTransfer)));
            }
        }
    }

    #endregion

    #region Transfer
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < UpdateInterval)
            return;

        var dt = _accumulator;
        _accumulator = 0f;

        // Channels not used since the last update get dropped so the dictionary doesn't grow forever.
        foreach (var (channel, group) in _channels)
        {
            if (group.Transmitters.Count == 0 && group.Receivers.Count == 0)
                _channels.Remove(channel);

            group.Transmitters.Clear();
            group.Receivers.Clear();
        }

        var query = EntityQueryEnumerator<WirelessPowerComponent, BatteryComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var battery, out var xform))
        {
            if (!xform.Anchored)
            {
                SetLinked((uid, comp), 0);
                comp.LastTransfer = 0f;
                continue;
            }

            if (!_channels.TryGetValue(comp.Channel, out var group))
            {
                group = new ChannelGroup();
                _channels[comp.Channel] = group;
            }

            var device = new Device(uid, comp, battery);
            if (comp.Mode == WirelessPowerMode.Transmitter)
                group.Transmitters.Add(device);
            else
                group.Receivers.Add(device);
        }

        foreach (var group in _channels.Values)
        {
            ProcessChannel(group, dt);
        }
    }

    private void ProcessChannel(ChannelGroup group, float dt)
    {
        var txs = group.Transmitters;
        var rxs = group.Receivers;

        foreach (var tx in txs)
        {
            SetLinked((tx.Uid, tx.Comp), rxs.Count);
            tx.Comp.LastTransfer = 0f;
        }

        foreach (var rx in rxs)
        {
            SetLinked((rx.Uid, rx.Comp), txs.Count);
            rx.Comp.LastTransfer = 0f;
        }

        if (txs.Count == 0 || rxs.Count == 0)
            return;

        // Every device on the channel beyond the first pair makes transfer less efficient.
        var extraDevices = Math.Max(0, txs.Count + rxs.Count - 2);

        // How much the transmitters can drain, and how much of that actually arrives.
        var totalDeliverable = 0f;
        group.Drainable.Clear();
        group.Overhead.Clear();
        foreach (var tx in txs)
        {
            _battery.UseCharge((tx.Uid, tx.Battery), tx.Comp.IdleDraw * dt);

            var overhead = 1f + tx.Comp.BaseLoss + tx.Comp.LossPerExtraDevice * extraDevices;
            var canDrain = MathF.Min(_battery.GetCharge(tx.Uid).Charge, tx.Comp.MaxTransferRate * dt);

            group.Drainable.Add(canDrain);
            group.Overhead.Add(overhead);
            totalDeliverable += canDrain / overhead;
            tx.Comp.LastEfficiency = 1f / overhead;
        }

        // How much the receivers want.
        var totalDemand = 0f;
        group.Demand.Clear();
        foreach (var rx in rxs)
        {
            var (charge, maxCharge) = _battery.GetCharge(rx.Uid);
            var space = maxCharge - charge;
            var want = MathF.Max(0f, MathF.Min(space, rx.Comp.MaxTransferRate * dt));
            group.Demand.Add(want);
            totalDemand += want;
        }

        var delivered = MathF.Min(totalDeliverable, totalDemand);
        if (delivered <= 0f)
            return;

        // Each transmitter supplies in proportion to what it could deliver.
        var txFraction = delivered / totalDeliverable;
        for (var i = 0; i < txs.Count; i++)
        {
            var drain = group.Drainable[i] * txFraction;
            _battery.UseCharge((txs[i].Uid, txs[i].Battery), drain);
            txs[i].Comp.LastTransfer = drain / group.Overhead[i] / dt;
        }

        // Each receiver gets power in proportion to what it asked for.
        var rxFraction = delivered / totalDemand;
        for (var i = 0; i < rxs.Count; i++)
        {
            var gain = group.Demand[i] * rxFraction;
            _battery.ChangeCharge((rxs[i].Uid, rxs[i].Battery), gain);
            rxs[i].Comp.LastTransfer = gain / dt;
        }
    }

    private void SetLinked(Entity<WirelessPowerComponent> ent, int count)
    {
        var wasLinked = ent.Comp.LinkedCount > 0;
        ent.Comp.LinkedCount = count;

        if (wasLinked != count > 0)
            _appearance.SetData(ent, WirelessPowerVisuals.Linked, count > 0);
    }
    #endregion

    private readonly record struct Device(EntityUid Uid, WirelessPowerComponent Comp, BatteryComponent Battery);

    private sealed class ChannelGroup
    {
        public readonly List<Device> Transmitters = new();
        public readonly List<Device> Receivers = new();
        public readonly List<float> Drainable = new();
        public readonly List<float> Overhead = new();
        public readonly List<float> Demand = new();
    }
}
