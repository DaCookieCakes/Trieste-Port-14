using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Destructible;
using Content.Server.DoAfter;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Radio.EntitySystems;
using Content.Shared._TP.Power.Generator;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Explosion.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Content.Shared.Verbs;

namespace Content.Server._TP.Power.Generator;

/// <summary>
///     Systems handling the Storm Array.
///     This is similar to the TEG coolant loop, absorbing heat and transferring it to pipe gas.
///     Created by Cookie for Trieste Port 14.
/// </summary>
public sealed class StormArraySystem : EntitySystem
{
    // Pipe names from the Storm Array entity.
    private const string NodeNameInlet = "inlet";
    private const string NodeNameOutlet = "outlet";

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StormArrayComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<StormArrayComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StormArrayComponent, GetVerbsEvent<ActivationVerb>>(OnVerbActivation);
        SubscribeLocalEvent<StormArrayComponent, StormArrayDoAfterEvent>(OnStormArrayEnabled);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnStormArrayEnabled(Entity<StormArrayComponent> ent, ref StormArrayDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ent.Comp.Enabled = true;

        _popup.PopupEntity(Loc.GetString("storm-array-message-enabled"), ent.Owner, args.User, PopupType.Medium);

        _appearance.SetData(ent.Owner, StormArrayVisuals.Idle, false);
        _appearance.SetData(ent.Owner, StormArrayVisuals.Active, true);
    }

    private void OnVerbActivation(Entity<StormArrayComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.Enabled)
            return;

        var user = args.User;
        var verb = new ActivationVerb()
        {
            Act = () => HandleEnabling(ent, user),
            Text = Loc.GetString("storm-array-verb-enable"),
            Message = Loc.GetString("storm-array-message-enabling"),
        };

        args.Verbs.Add(verb);
    }

    private void HandleEnabling(Entity<StormArrayComponent> ent, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("storm-array-message-enabling"), ent.Owner, user, PopupType.MediumCaution);

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            TimeSpan.FromSeconds(10),
            new StormArrayDoAfterEvent(),
            ent.Owner,
            ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnExamined(Entity<StormArrayComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Enabled)
        {
            args.PushMarkup(Loc.GetString("storm-array-examine-disabled"));
            return;
        }

        if (!TryComp<TemperatureComponent>(ent, out var temp))
            return;

        var comp = ent.Comp;

        // Show the internal temperature in both Kelvin and Celsius
        var tempC = temp.CurrentTemperature - 273.15f;
        args.PushMarkup(Loc.GetString("storm-array-examine-temperature",
            ("tempK", temp.CurrentTemperature.ToString("F1")),
            ("tempC", tempC.ToString("F1"))));

        // Show a status message if available
        if (!string.IsNullOrEmpty(comp.StatusMessage))
        {
            args.PushMarkup(Loc.GetString("storm-array-examine-status",
                ("status", comp.StatusMessage)));
        }

        // Display the cooling stats
        if (comp.LastCoolingRate > 0)
        {
            args.PushMarkup(Loc.GetString("storm-array-examine-cooling",
                ("rate", (comp.LastCoolingRate / 1000).ToString("F1"))));
        }
    }

    private void OnAtmosUpdate(Entity<StormArrayComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (!TryComp<TemperatureComponent>(ent, out var tempComp))
            return;

        var comp = ent.Comp;

        // Heat ourselves
        tempComp.CurrentTemperature += comp.HeatGenerationRate * args.dt / comp.SelfHeatCapacity;

        // Now update the coolant AFTER the heating, in a separate function.
        UpdateCoolant(ent, ref args);

        // Then we announce if the temperature is too high, based on the thresholds.
        // This is also a separate function UNLESS the temperature is 500,
        // in which case it will explode.
        Announcement(ent,
            Loc.GetString("storm-array-alert-1"),
            tempComp.CurrentTemperature >= 411,
            ref comp.FirstAnnouncement);

        Announcement(ent,
            Loc.GetString("storm-array-alert-2"),
            tempComp.CurrentTemperature >= 822,
            ref comp.SecondAnnouncement);

        Announcement(ent,
            Loc.GetString("storm-array-alert-3"),
            tempComp.CurrentTemperature >= 1233,
            ref comp.ThirdAnnouncement);

        // This part handles the explosion at 1644 degrees.
        // If the explosion component doesn't exist, however, we return. (This shouldn't happen!)
        if (!TryComp<ExplosiveComponent>(ent, out var explosive))
            return;

        if (tempComp.CurrentTemperature >= 1644)
            _destructible.ExplosionSystem.TriggerExplosive(ent.Owner, explosive, true, explosive.TotalIntensity);
    }

    private void Announcement(Entity<StormArrayComponent> ent, string msg, bool when, ref bool announcementFlag)
    {
        if (!when || announcementFlag)
            return;

        _radio.SendRadioMessage(ent.Owner, msg, "Engineering", ent.Owner);

        announcementFlag = true;
    }

    private void UpdateCoolant(Entity<StormArrayComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        var comp = ent.Comp;
        if (!TryComp<TemperatureComponent>(ent, out var temp))
            return;

        if (!_nodeContainerQuery.TryGetComponent(ent, out var nodeContainer))
            return;

        if (!nodeContainer.Nodes.TryGetValue(NodeNameInlet, out var inletNode) ||
            !nodeContainer.Nodes.TryGetValue(NodeNameOutlet, out var outletNode))
            return;

        var inlet = (PipeNode)inletNode;
        var outlet = (PipeNode)outletNode;

        if (inlet.Air.TotalMoles <= 0)
        {
            comp.LastCoolingRate = 0;
            comp.LastCoolantFlow = 0;
            comp.StatusMessage = Loc.GetString("storm-array-status-no-coolant");
            return;
        }

        var transferGas = inlet.Air.RemoveRatio(0.5f);

        if (transferGas.TotalMoles <= 0)
            return;

        var gasHeatCapacity = _atmosphere.GetHeatCapacity(transferGas, true);

        if (gasHeatCapacity <= 0)
        {
            comp.StatusMessage = Loc.GetString("storm-array-status-no-capacity");
            _atmosphere.Merge(outlet.Air, transferGas); // don't eat the gas
            return;
        }

        var tempDifference = temp.CurrentTemperature - transferGas.Temperature;

        if (tempDifference <= 0)
        {
            comp.StatusMessage = Loc.GetString("storm-array-status-warmer-coolant",
                ("temp1", transferGas.Temperature.ToString("F1")),
                ("temp2", temp.CurrentTemperature.ToString("F1")));
            _atmosphere.Merge(outlet.Air, transferGas); // still pass it through
            return;
        }

        // How much heat can we actually transfer this tick
        var maxHeatFromTempDiff = tempDifference * gasHeatCapacity;
        var heatTransferred = maxHeatFromTempDiff * comp.CoolingEfficiency;

        // Cool the array, heat the gas
        temp.CurrentTemperature -= heatTransferred / comp.SelfHeatCapacity;
        transferGas.Temperature += heatTransferred / gasHeatCapacity;

        // Push heated gas to outlet
        _atmosphere.Merge(outlet.Air, transferGas);

        comp.LastCoolingRate = heatTransferred / args.dt;
        comp.LastCoolantFlow = transferGas.TotalMoles;
        comp.StatusMessage = Loc.GetString("storm-array-status-cooling",
            ("last1", (comp.LastCoolingRate / 1000).ToString("F2")),
            ("last2", (comp.LastCoolantFlow / 1000).ToString("F2")));
    }
}
