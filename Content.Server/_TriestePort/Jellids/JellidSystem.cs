using Content.Server.Atmos.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared._TriestePort.Jellids;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._TriestePort.Jellids;

/// <summary>
///     The JellidComponent system handling everything related to power.
///     Such as charging, draining, and alerts.
/// </summary>
public sealed partial class JellidSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;

    // The jellid-proof gloves tag proto ID.
    private static readonly ProtoId<TagPrototype> FireproofTag = "TP14TagJellidProofClothing";

    // Track the previous charge to detect if this Jellid is charging.
    private readonly Dictionary<EntityUid, float> _previousCharges = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JellidComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<JellidComponent, ElectrocutedEvent>(OnElectrocution);

        // Proper charging.
        SubscribeLocalEvent<BatteryComponent, UseInHandEvent>(OnUseBatteryInHand);
        SubscribeLocalEvent<JellidComponent, JellidBatteryDoAfterEvent>(OnJellidDoAfter);
    }

    /// <summary>
    ///     What the do-after triggers when the user uses the battery.
    ///     This handles the battery power transfer.
    /// </summary>
    /// <param name="ent">JellidComponent Entity</param>
    /// <param name="args">JellidBatteryDoAfterEvent Arguments</param>
    private void OnJellidDoAfter(Entity<JellidComponent> ent, ref JellidBatteryDoAfterEvent args)
    {
        // If the do-after is canceled, or the user has already used the battery, return.
        if (args.Cancelled || args.Handled)
            return;

        // If the:
        // 1. held item is wieldable, return.
        // 2. held item is NOT a battery, return.
        // 3. jellid (user) is NOT a jellid, return.
        // 4. jellid (user) is NOT a battery, return.
        if (HasComp<WieldableComponent>(args.Used))
            return;

        if (!TryComp<BatteryComponent>(args.Used, out var batteryComp))
            return;

        if (!TryComp<JellidComponent>(args.User, out var jellidComp))
            return;

        if (!TryComp<BatteryComponent>(args.User, out var jellidBatteryComp))
            return;

        var batLevel = _battery.GetCharge((args.Used.Value, batteryComp));
        var jelLevel = _battery.GetCharge((args.User, jellidBatteryComp));

        // Now get the battery's max charge and multiply it by the JellidComponent drain percent.
        // If the battery's current charge is less than the drain, return with a popup.
        // Otherwise, drain the battery and add the charge to the Jellid's battery.
        var drain = batteryComp.MaxCharge * jellidComp.DrainPercent;
        if (batLevel < drain)
        {
            _popup.PopupEntity(Loc.GetString("jellid-used-failed"), args.User, args.User);
            args.Repeat = false;

            return;
        }

        if (jelLevel + drain >= jellidBatteryComp.MaxCharge)
        {
            args.Repeat = false;
            return;
        }

        _battery.SetCharge(args.Used.Value, batLevel - drain);
        _battery.SetCharge(args.User, jelLevel + drain);

        _audio.PlayPvs(jellidComp.BatteryUseSound, ent.Owner, AudioParams.Default.WithLoop(false).WithVolume(-3));
        _popup.PopupEntity(Loc.GetString("jellid-used-success"), args.User, args.User);

        args.Repeat = batLevel >= drain;
        args.Handled = true;
    }

    private void OnUseBatteryInHand(Entity<BatteryComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<JellidComponent>(args.User, out _))
            return;

        // Check for fireproof gloves before charging. They also block charging, as requested by Pix.
        if (_inventory.TryGetSlotEntity(args.User, "gloves", out var glovesUid)
            && _tag.HasTag(glovesUid.Value, FireproofTag))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, 1f, new JellidBatteryDoAfterEvent(), args.User, null, ent.Owner)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnElectrocution(Entity<JellidComponent> ent, ref ElectrocutedEvent args)
    {
        if (!TryComp<BatteryComponent>(ent.Owner, out var battery))
            return;

        var jelLevel = _battery.GetCharge((ent.Owner, battery));

        var chargeGain = 100f * args.SiemensCoefficient;
        _battery.SetCharge(ent.Owner, Math.Min(jelLevel + chargeGain, battery.MaxCharge));
    }

    private void OnShutdown(Entity<JellidComponent> ent, ref ComponentShutdown args)
    {
        _previousCharges.Remove(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JellidComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Timing check so this doesn't run every tick.
            if (_timing.CurTime < comp.NextPowerDrain)
                continue;

            comp.NextPowerDrain = _timing.CurTime + TimeSpan.FromSeconds(1f);

            if (!TryComp<BatteryComponent>(uid, out var jellidBattery))
                continue;

            // Alert check!
            // If the internal battery is below 300, display an empty battery alert.
            // Otherwise, display a battery alert with the charge percentage.
            UpdateAlerts(uid, comp, jellidBattery);

            // Held item check!
            // If the user DOES NOT have gloves on and a battery is held, it will slowly drain into the Jellid.
            // Also check if the user has BURNABLE ITEMS in their hands. If so, burn it to ash.
            var hasFireproofGloves = _inventory.TryGetSlotEntity(uid, "gloves", out var glovesUid)
                                     && _tag.HasTag(glovesUid.Value, FireproofTag);

            if (!hasFireproofGloves)
            {
                UpdateHeldBatteries(uid, jellidBattery);
                UpdateHeldBurnables(uid);
            }

            // Damage check!
            // If the internal battery is below 20, damage the Jellid and add it to a previous charge dict.
            // We only damage the Jellid if it's NOT charging. we deal 1 slash damage.
            UpdatePowerDamage(uid, jellidBattery);
        }
    }

    private void UpdateHeldBurnables(EntityUid uid)
    {
        if (_hands.GetActiveItem(uid) is not { } heldItem)
            return;

        if (!TryComp<FlammableComponent>(heldItem, out var flammable))
            return;

        _flammable.AdjustFireStacks(heldItem, flammable.FireStacks, flammable);
        if (flammable.FireStacks >= 0)
            _flammable.Ignite(heldItem, heldItem, flammable, uid);
    }

    /// <summary>
    ///     Helper method to damage the player if their charge is below 100.
    ///     NOTE: Needs testing if 100 is too high!
    /// </summary>
    /// <param name="uid">Jellid's UID</param>
    /// <param name="jellidBattery">Jellid's BatteryComponent</param>
    private void UpdatePowerDamage(EntityUid uid, BatteryComponent jellidBattery)
    {
        var jelLevel = _battery.GetCharge((uid, jellidBattery));

        const float damageCharge = 100f;
        if (jelLevel >= damageCharge)
        {
            _previousCharges[uid] = jelLevel;
            return;
        }

        var isCharging = _previousCharges.TryGetValue(uid, out var prevCharge) && jelLevel > prevCharge;
        if (isCharging)
            return;

        var damage = new DamageSpecifier
        {
            DamageDict = { ["Shock"] = 1f }
        };
        _damageable.TryChangeDamage(uid, damage, origin: uid);
    }

    /// <summary>
    ///     Helper method to drain HELD batteries passively into the player.
    ///     This is separate from 'eating' power, but it still feeds them.
    /// </summary>
    /// <param name="uid">Jellid's UID</param>
    /// <param name="jellidBattery">Jellid's BatteryComponent</param>
    private void UpdateHeldBatteries(EntityUid uid, BatteryComponent jellidBattery)
    {
        foreach (var hand in _hands.EnumerateHands(uid))
        {
            if (!_hands.TryGetHeldItem(uid, hand, out var heldItem))
                continue;

            if (!TryComp<BatteryComponent>(heldItem, out var batteryComp))
                continue;

            var batLevel = _battery.GetCharge((heldItem.Value, batteryComp));
            var jelLevel = _battery.GetChargeLevel((uid, jellidBattery));

            // Drain at a rate of a constant 2.5 power.
            _battery.SetCharge(heldItem.Value, batLevel - 2.5f);
            _battery.SetCharge(uid,jelLevel + 2.5f);
        }
    }

    /// <summary>
    ///     Helper method to update the Jellid's battery alerts.
    ///     If below 300, an empty battery is displayed. Otherwise, display a numbered 10-0 battery.
    /// </summary>
    /// <param name="uid">Jellid's UID</param>
    /// <param name="comp">Jellid's JellidComponent</param>
    /// <param name="jellidBattery">Jellid's BatteryComponent</param>
    private void UpdateAlerts(EntityUid uid, JellidComponent comp, BatteryComponent jellidBattery)
    {
        var jelLevel = _battery.GetChargeLevel((uid, jellidBattery));

        const float alertChange = 300f;
        var chargePercent = (short) MathF.Round(jelLevel / jellidBattery.MaxCharge * 10f);
        if (jelLevel > alertChange)
        {
            _alerts.ClearAlert(uid, comp.NoBatteryAlert);
            _alerts.ShowAlert(uid, comp.BatteryAlert, chargePercent);
        }
        else
        {
            _alerts.ClearAlert(uid, comp.BatteryAlert);
            _alerts.ShowAlert(uid, comp.NoBatteryAlert);
        }
    }
}
