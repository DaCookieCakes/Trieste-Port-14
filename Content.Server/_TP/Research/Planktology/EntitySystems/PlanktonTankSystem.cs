using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared._TP.Research.Planktology;
using Content.Shared._TP.Research.Planktology.Components;
using Content.Shared._TP.Research.Planktology.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._TP.Research.Planktology.EntitySystems;

/// <summary>
///     The culture tank: its controls, beaker and container slots, and the research form.
/// </summary>
public sealed partial class PlanktonTankSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private PlanktonSystem _plankton = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    /// <summary>
    ///     Most units the inject buttons may ask for, so a modified client can't dump a whole beaker at once.
    /// </summary>
    private const int MaxInject = 10;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Sizes, hunger and the radiation timer change constantly, so open UIs get a regular refresh.
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PlanktonTankComponent>();
        while (query.MoveNext(out var uid, out var tank))
        {
            if (now < tank.NextUiUpdate)
                continue;

            tank.NextUiUpdate = now + tank.UiUpdateInterval;
            if (_ui.IsUiOpen(uid, PlanktonTankUiKey.Key))
                UpdateUi((uid, tank));
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PlanktonTankComponent> ent, ref MapInitEvent args)
    {
        UpdateLight(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<PlanktonTankComponent> ent, ref PowerChangedEvent args)
    {
        UpdateLight(ent);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<PlanktonTankComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnItemInserted(Entity<PlanktonTankComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnItemRemoved(Entity<PlanktonTankComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUi(ent);
    }

    private void UpdateLight(Entity<PlanktonTankComponent> ent)
    {
        _light.SetEnabled(ent, ent.Comp.LightsOn && _power.IsPowered(ent.Owner));
    }

    private bool CheckPower(Entity<PlanktonTankComponent> ent, EntityUid? user)
    {
        if (_power.IsPowered(ent.Owner))
            return true;

        if (user != null)
            _popup.PopupEntity(Loc.GetString("plankton-tank-no-power"), ent, user.Value);

        return false;
    }

    #region Controls

    [SubscribeLocalEvent]
    private void OnToggleLights(Entity<PlanktonTankComponent> ent, ref PlanktonTankToggleLightsMessage args)
    {
        if (!CheckPower(ent, args.Actor))
            return;

        ent.Comp.LightsOn = !ent.Comp.LightsOn;
        Dirty(ent);
        UpdateLight(ent);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnAdjustTemperature(Entity<PlanktonTankComponent> ent, ref PlanktonTankAdjustTemperatureMessage args)
    {
        if (!CheckPower(ent, args.Actor))
            return;

        var delta = args.Raise ? ent.Comp.TemperatureStep : -ent.Comp.TemperatureStep;
        ent.Comp.Temperature = Math.Clamp(ent.Comp.Temperature + delta, ent.Comp.MinTemperature, ent.Comp.MaxTemperature);
        Dirty(ent);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnRadiationFlash(Entity<PlanktonTankComponent> ent, ref PlanktonTankRadiationFlashMessage args)
    {
        if (!CheckPower(ent, args.Actor))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextRadiationFlash)
            return;

        ent.Comp.NextRadiationFlash = now + ent.Comp.RadiationFlashCooldown;
        Dirty(ent);

        if (TryComp<PlanktonComponent>(ent, out var plankton))
            _plankton.Irradiate((ent, plankton), ent.Comp.RadiationFlashDose);

        _popup.PopupEntity(Loc.GetString("plankton-tank-radiation-flash"), ent, args.Actor);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnInject(Entity<PlanktonTankComponent> ent, ref PlanktonTankInjectMessage args)
    {
        if (args.Amount <= 0
            || _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.BeakerSlotId) is not { } beaker
            || !_solutions.TryGetFitsInDispenser(beaker, out var beakerSoln, out var beakerSolution)
            || !_solutions.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var tankSoln, out var tankSolution))
            return;

        var amount = FixedPoint2.Min(Math.Min(args.Amount, MaxInject), beakerSolution.Volume, tankSolution.AvailableVolume);
        if (amount <= 0)
            return;

        var poured = _solutions.SplitSolution(beakerSoln.Value, amount);
        _solutions.TryAddSolution(tankSoln.Value, poured);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnDump(Entity<PlanktonTankComponent> ent, ref PlanktonTankDumpMessage args)
    {
        if (!TryGetSamples(ent, out var tank, out var container))
            return;

        var space = ent.Comp.MaxSpecies - tank.Value.Comp.Species.Count;
        if (space < container.Value.Comp.Species.Count)
        {
            _popup.PopupEntity(Loc.GetString("plankton-tank-full", ("max", ent.Comp.MaxSpecies)), ent, args.Actor);
            return;
        }

        _plankton.TransferAll(container.Value, tank.Value);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnExtract(Entity<PlanktonTankComponent> ent, ref PlanktonTankExtractMessage args)
    {
        if (!TryGetSamples(ent, out var tank, out var container))
            return;

        _plankton.TransferAll(tank.Value, container.Value);
        UpdateUi(ent);
    }

    private bool TryGetSamples(Entity<PlanktonTankComponent> ent,
        [NotNullWhen(true)] out Entity<PlanktonComponent>? tank,
        [NotNullWhen(true)] out Entity<PlanktonComponent>? container)
    {
        tank = null;
        container = null;

        if (!TryComp<PlanktonComponent>(ent, out var tankComp)
            || _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.ContainerSlotId) is not { } item
            || !TryComp<PlanktonComponent>(item, out var containerComp))
            return false;

        tank = (ent, tankComp);
        container = (item, containerComp);
        return true;
    }

    #endregion

    #region Research form

    /// <summary>
    ///     Finds the species a form message is about. Submitted forms are locked.
    /// </summary>
    private bool TryGetFormSpecies(Entity<PlanktonTankComponent> ent,
        PlanktonTankSpeciesMessage msg,
        [NotNullWhen(true)] out PlanktonComponent? plankton,
        [NotNullWhen(true)] out PlanktonSpecies? species)
    {
        species = null;
        if (!TryComp(ent, out plankton))
            return false;

        if (msg.Index < 0 || msg.Index >= plankton.Species.Count)
            return false;

        species = plankton.Species[msg.Index];
        return species.Number == msg.Number && !species.Submitted;
    }

    [SubscribeLocalEvent]
    private void OnGuessDiet(Entity<PlanktonTankComponent> ent, ref PlanktonTankGuessDietMessage args)
    {
        if (!TryGetFormSpecies(ent, args, out var plankton, out var species) || !_proto.HasIndex(args.Diet))
            return;

        species.GuessedDiet = species.GuessedDiet == args.Diet ? null : (ProtoId<PlanktonDietPrototype>?) args.Diet;
        species.CrossedOut.Remove(args.Diet);
        Dirty(ent, plankton);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnGuessCharacteristic(Entity<PlanktonTankComponent> ent, ref PlanktonTankGuessCharacteristicMessage args)
    {
        if (!TryGetFormSpecies(ent, args, out var plankton, out var species) || !_proto.HasIndex(args.Characteristic))
            return;

        if (!species.GuessedCharacteristics.Remove(args.Characteristic))
        {
            // The form only has three lines.
            if (species.GuessedCharacteristics.Count >= 3)
                return;

            species.GuessedCharacteristics.Add(args.Characteristic);
            species.CrossedOut.Remove(args.Characteristic);
        }

        Dirty(ent, plankton);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnCrossOut(Entity<PlanktonTankComponent> ent, ref PlanktonTankCrossOutMessage args)
    {
        if (!TryGetFormSpecies(ent, args, out var plankton, out var species))
            return;

        var isDiet = _proto.HasIndex<PlanktonDietPrototype>(args.Id);
        if (!isDiet && !_proto.HasIndex<PlanktonCharacteristicPrototype>(args.Id))
            return;

        if (!species.CrossedOut.Remove(args.Id))
        {
            species.CrossedOut.Add(args.Id);

            // Ruling something out also takes it off the form.
            if (isDiet && species.GuessedDiet?.Id == args.Id)
                species.GuessedDiet = null;
            else
                species.GuessedCharacteristics.Remove(args.Id);
        }

        Dirty(ent, plankton);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnSubmit(Entity<PlanktonTankComponent> ent, ref PlanktonTankSubmitMessage args)
    {
        if (!TryGetFormSpecies(ent, args, out var plankton, out var species))
            return;

        if (!species.Alive || species.Size < ent.Comp.MinimumSubmitSize || species.GuessedDiet == null)
            return;

        if (!_research.TryGetClientServer(ent, out var server, out _))
        {
            _popup.PopupEntity(Loc.GetString("plankton-tank-no-server"), ent, args.Actor);
            return;
        }

        var correct = species.GuessedDiet == species.Diet
            && species.GuessedCharacteristics.Count == species.Characteristics.Count
            && species.GuessedCharacteristics.All(species.Characteristics.Contains);

        species.Submitted = true;
        species.SubmittedCorrect = correct;
        Dirty(ent, plankton);

        if (correct)
        {
            var points = ent.Comp.BaseResearchPoints
                + species.Characteristics.Sum(c => _proto.Index(c).Points) * ent.Comp.ResearchPointsPerCharacteristicPoint;

            _research.ModifyServerPoints(server.Value, points);
            _popup.PopupEntity(Loc.GetString("plankton-tank-submit-correct", ("points", points)), ent, args.Actor);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("plankton-tank-submit-incorrect"), ent, args.Actor, PopupType.MediumCaution);
        }

        UpdateUi(ent);
    }

    #endregion

    private void UpdateUi(Entity<PlanktonTankComponent> ent)
    {
        if (!TryComp<PlanktonComponent>(ent, out var plankton))
            return;

        string? beakerName = null;
        FixedPoint2 beakerVolume = 0, beakerMax = 0, tankVolume = 0, tankMax = 0;

        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.BeakerSlotId) is { } beaker)
        {
            beakerName = Name(beaker);
            if (_solutions.TryGetFitsInDispenser(beaker, out _, out var beakerSolution))
            {
                beakerVolume = beakerSolution.Volume;
                beakerMax = beakerSolution.MaxVolume;
            }
        }

        if (_solutions.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var tankSolution))
        {
            tankVolume = tankSolution.Volume;
            tankMax = tankSolution.MaxVolume;
        }

        var containerItem = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.ContainerSlotId);
        var containerCount = containerItem != null && TryComp<PlanktonComponent>(containerItem, out var carried)
            ? carried.Species.Count
            : 0;

        _ui.SetUiState(ent.Owner,
            PlanktonTankUiKey.Key,
            new PlanktonTankBoundUserInterfaceState(
                plankton.Species,
                _power.IsPowered(ent.Owner),
                ent.Comp.LightsOn,
                ent.Comp.Temperature,
                ent.Comp.MinTemperature,
                ent.Comp.MaxTemperature,
                ent.Comp.NextRadiationFlash,
                beakerName,
                beakerVolume,
                beakerMax,
                tankVolume,
                tankMax,
                containerItem != null,
                containerCount,
                ent.Comp.MaxSpecies,
                ent.Comp.MinimumSubmitSize,
                _research.TryGetClientServer(ent, out _, out _)));
    }

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
