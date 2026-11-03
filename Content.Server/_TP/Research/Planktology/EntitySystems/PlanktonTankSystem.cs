using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Research.Planktology.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Timing;

namespace Content.Server._TP.Research.Planktology.EntitySystems;

/// <summary>
///     Culture tank controls.
/// </summary>
public sealed partial class PlanktonTankSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private PlanktonSystem _plankton = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PlanktonTankComponent> ent, ref MapInitEvent args)
    {
        UpdateLight(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<PlanktonTankComponent> ent, ref PowerChangedEvent args)
    {
        UpdateLight(ent);
    }

    private void UpdateLight(Entity<PlanktonTankComponent> ent)
    {
        _light.SetEnabled(ent, ent.Comp.LightsOn && _power.IsPowered(ent.Owner));
    }

    #region Pouring

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PlanktonTankComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !HasComp<PlanktonContainerComponent>(args.Used)
            || !TryComp<PlanktonComponent>(args.Used, out var carried)
            || !TryComp<PlanktonComponent>(ent, out var tank))
            return;

        args.Handled = true;
        Entity<PlanktonComponent> container = (args.Used, carried);
        Entity<PlanktonComponent> tankSample = (ent, tank);

        // A full container pours into the tank, an empty one scoops the tank out.
        if (carried.Species.Count > 0)
        {
            var count = _plankton.TransferAll(container, tankSample);
            _popup.PopupEntity(Loc.GetString("plankton-tank-poured", ("count", count)), ent, args.User);
        }
        else if (tank.Species.Count > 0)
        {
            var count = _plankton.TransferAll(tankSample, container);
            _popup.PopupEntity(Loc.GetString("plankton-tank-collected", ("count", count)), ent, args.User);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("plankton-tank-both-empty"), ent, args.User);
        }
    }

    #endregion

    #region Controls

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<PlanktonTankComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(ent.Comp.LightsOn ? "plankton-tank-verb-lights-off" : "plankton-tank-verb-lights-on"),
            Act = () => ToggleLights(ent, user),
            Priority = 3,
        });

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("plankton-tank-verb-heat"),
            Act = () => AdjustTemperature(ent, user, ent.Comp.TemperatureStep),
            Priority = 2,
        });

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("plankton-tank-verb-cool"),
            Act = () => AdjustTemperature(ent, user, -ent.Comp.TemperatureStep),
            Priority = 1,
        });

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("plankton-tank-verb-radiation"),
            Act = () => RadiationFlash(ent, user),
            Priority = 0,
        });
    }

    private bool CheckPower(Entity<PlanktonTankComponent> ent, EntityUid user)
    {
        if (_power.IsPowered(ent.Owner))
            return true;

        _popup.PopupEntity(Loc.GetString("plankton-tank-no-power"), ent, user);
        return false;
    }

    private void ToggleLights(Entity<PlanktonTankComponent> ent, EntityUid user)
    {
        if (!CheckPower(ent, user))
            return;

        ent.Comp.LightsOn = !ent.Comp.LightsOn;
        Dirty(ent);
        UpdateLight(ent);
    }

    private void AdjustTemperature(Entity<PlanktonTankComponent> ent, EntityUid user, float delta)
    {
        if (!CheckPower(ent, user))
            return;

        ent.Comp.Temperature = Math.Clamp(ent.Comp.Temperature + delta, ent.Comp.MinTemperature, ent.Comp.MaxTemperature);
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("plankton-tank-temperature",
                ("temperature", (int) MathF.Round(ent.Comp.Temperature - 273.15f))),
            ent,
            user);
    }

    private void RadiationFlash(Entity<PlanktonTankComponent> ent, EntityUid user)
    {
        if (!CheckPower(ent, user))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextRadiationFlash)
        {
            _popup.PopupEntity(Loc.GetString("plankton-tank-radiation-cooldown"), ent, user);
            return;
        }

        ent.Comp.NextRadiationFlash = now + ent.Comp.RadiationFlashCooldown;

        if (TryComp<PlanktonComponent>(ent, out var plankton))
            _plankton.Irradiate((ent, plankton), ent.Comp.RadiationFlashDose);

        _popup.PopupEntity(Loc.GetString("plankton-tank-radiation-flash"), ent, user);
    }

    #endregion

    [SubscribeLocalEvent]
    private void OnExamined(Entity<PlanktonTankComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.LightsOn ? "plankton-tank-examine-lights-on" : "plankton-tank-examine-lights-off"));
        args.PushMarkup(Loc.GetString("plankton-tank-examine-temperature",
            ("temperature", (int) MathF.Round(ent.Comp.Temperature - 273.15f))));
    }
}
