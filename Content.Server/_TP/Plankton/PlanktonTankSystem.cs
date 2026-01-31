using System.Linq;
using Content.Server.Power.Components;
using Content.Shared._TP.Plankton;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server._TP.Plankton;

/// <summary>
///     Handles the planktology tank.
/// </summary>
public sealed class PlanktonTankSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private const float UpdateInterval = 1f;
    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanktonTankComponent, ComponentInit>(OnTankInit);
        SubscribeLocalEvent<PlanktonTankComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<PlanktonTankComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlanktonTankComponent, GetVerbsEvent<AlternativeVerb>>(AddTemperatureVerbs);
        SubscribeLocalEvent<PlanktonTankComponent, GetVerbsEvent<Verb>>(AddExtractAndInsertVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateInterval)
        {
            foreach (var tank in EntityQuery<PlanktonTankComponent>())
            {
                UpdateTemperature(tank.Owner, tank);
                CheckPlanktonSurvival(tank.Owner, tank);
            }
            _updateTimer = 0f;
        }
    }

    /// <summary>
    ///     Ensures the tank has a plankton component, and sets the initial power state.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnTankInit(EntityUid uid, PlanktonTankComponent component, ComponentInit args)
    {
        EnsureComp<PlanktonComponent>(uid);

        component.IsPowered = TryComp<ApcPowerReceiverComponent>(uid, out var receiver) && receiver.Powered;
    }

    private void OnPowerChanged(EntityUid uid, PlanktonTankComponent component, ref PowerChangedEvent args)
    {
        component.IsPowered = args.Powered;
    }

    /// <summary>
    ///     Updates the Tank's temperature.
    /// </summary>
    /// <param name="uid">The Tank UID</param>
    /// <param name="component">The Tank Component</param>
    private void UpdateTemperature(EntityUid uid, PlanktonTankComponent component)
    {
        // If the component IS NOT power, we return early.
        if (!component.IsPowered)
            return;

        var tempDiff = component.TargetTemperature - component.CurrentTemperature;

        // Now we check if the temperature difference is below 0.1 via ABS.
        // If so, we set the receiver load to idle consumption (500) and return.
        // If not, however, we set the receiver load to the ACTIVE state. (1000)
        if (Math.Abs(tempDiff) < 0.1f)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            {
                receiver.Load = component.IdlePowerConsumption;
            }
            return;
        }

        if (TryComp<ApcPowerReceiverComponent>(uid, out var activeReceiver))
        {
            activeReceiver.Load = component.ActivePowerConsumption;
        }

        // Now we check if the tempDiff IS ABOVE ZERO.
        // If it is, increase temp. Otherwise, decrease temp.
        if (tempDiff > 0)
        {
            component.CurrentTemperature += Math.Min(component.HeatingRate * UpdateInterval, tempDiff);
        }
        else
        {
            component.CurrentTemperature -= Math.Min(component.CoolingRate * UpdateInterval, Math.Abs(tempDiff));
        }
    }

    /// <summary>
    ///     Checks whether the plankton can survive.
    /// </summary>
    /// <param name="uid">The Tank UID</param>
    /// <param name="tank">The Tank Component</param>
    private void CheckPlanktonSurvival(EntityUid uid, PlanktonTankComponent tank)
    {
        if (!TryComp<PlanktonComponent>(uid, out var plankton))
            return;

        // Check if tank has enough SeaWater.
        // If not, we start to rapidly kill the plankton cultures.
        if (_solution.TryGetSolution(uid, tank.WaterSolutionName, out var solutionEnt, out var solution))
        {
            var seawaterAmount = 0f;

            foreach (var reagent in solution.Contents)
            {
                if (reagent.Reagent.Prototype == "SeaWater")
                {
                    seawaterAmount += reagent.Quantity.Float();
                }
            }

            if (seawaterAmount < tank.MinimumSeawaterVolume)
            {
                foreach (var species in plankton.SpeciesInstances)
                {
                    if (!species.IsAlive)
                        continue;

                    species.CurrentSize -= 2f;
                    plankton.DeadPlankton += 2f;

                    if (species.CurrentSize <= 0)
                    {
                        species.CurrentSize = 0;
                        species.IsAlive = false;
                        Log.Info($"{species.SpeciesName} died due to a lack of seawater in tank {uid}");
                    }
                }

                return;
            }
        }

        // Check the temperature tolerance for each species of plankton in the tank.
        foreach (var species in plankton.SpeciesInstances)
        {
            if (!species.IsAlive)
                continue;

            // Get the base tolerance from the plankton component,
            // then modify the tolerance based on characteristics.
            // Pyrophilic increases the tolerance, and Cryophilic decreases it.
            var toleranceLow = plankton.TemperatureToleranceLow;
            var toleranceHigh = plankton.TemperatureToleranceHigh;

            if ((species.Characteristics & PlanktonComponent.PlanktonCharacteristics.Pyrophilic) != 0)
            {
                toleranceHigh += 20f;
                toleranceLow -= 10f;
            }

            if ((species.Characteristics & PlanktonComponent.PlanktonCharacteristics.Cryophilic) != 0)
            {
                toleranceLow -= 20f;
                toleranceHigh += 10f;
            }

            // Now we check if the tolerance is above or below the low tolerance.
            // If so, we decrease the Plankton size and increase the dead count.
            if (tank.CurrentTemperature < toleranceLow || tank.CurrentTemperature > toleranceHigh)
            {
                species.CurrentSize -= 0.5f;
                plankton.DeadPlankton += 0.5f;

                if (species.CurrentSize <= 0)
                {
                    species.CurrentSize = 0;
                    species.IsAlive = false;
                    plankton.DeadPlankton += Math.Abs(species.CurrentSize);
                    Log.Info($"{species.SpeciesName} died due to temperature stress in tank {uid}");
                }
            }
        }
    }

    private void OnExamined(EntityUid uid, PlanktonTankComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var powerStatus = component.IsPowered ? "online" : "offline";
        args.PushMarkup(Loc.GetString("plankton-tank-examine-power", ("status", powerStatus)));
        args.PushMarkup(Loc.GetString("plankton-tank-examine-temp",
            ("current", $"{component.CurrentTemperature:F1}"),
            ("target", $"{component.TargetTemperature:F1}")));

        if (TryComp<PlanktonComponent>(uid, out var plankton))
        {
            var speciesCount = plankton.SpeciesInstances.Count;
            args.PushMarkup(Loc.GetString("plankton-tank-examine-species",
                ("count", speciesCount),
                ("max", component.MaxSpecies)));
        }
    }

    /// <summary>
    ///     Verbs for increasing or decreasing the Tank temperature.
    /// </summary>
    /// <param name="uid">The Tank UID</param>
    /// <param name="component">The Tank Component</param>
    /// <param name="args">GetVerbsEvent for Alternative Verbs arguments</param>
    private void AddTemperatureVerbs(EntityUid uid, PlanktonTankComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        // Basic checks first, like if we can access or can interact.
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Increase temperature
        AlternativeVerb increaseTemp = new()
        {
            Text = Loc.GetString("plankton-tank-increase-temp"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () =>
            {
                var newTemp = Math.Min(component.TargetTemperature + component.TemperatureStep, component.MaxTemperature);
                component.TargetTemperature = newTemp;
                _audio.PlayPvs(component.AdjustSound, uid);
                _popup.PopupEntity(Loc.GetString("plankton-tank-temp-increased", ("temp", $"{newTemp:F1}")), uid, args.User);
            },
            Priority = 1
        };

        // Decrease temperature
        AlternativeVerb decreaseTemp = new()
        {
            Text = Loc.GetString("plankton-tank-decrease-temp"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/snow.svg.192dpi.png")),
            Act = () =>
            {
                var newTemp = Math.Max(component.TargetTemperature - component.TemperatureStep, component.MinTemperature);
                component.TargetTemperature = newTemp;
                _audio.PlayPvs(component.AdjustSound, uid);
                _popup.PopupEntity(Loc.GetString("plankton-tank-temp-decreased", ("temp", $"{newTemp:F1}")), uid, args.User);
            },
            Priority = 0
        };

        args.Verbs.Add(increaseTemp);
        args.Verbs.Add(decreaseTemp);
    }

    /// <summary>
    ///     Verbs for extracting and inserting plankton.
    /// </summary>
    /// <param name="uid">The Tank UID</param>
    /// <param name="component">The Tank Component</param>
    /// <param name="args">GetVerbsEvent for Verb arguments</param>
    private void AddExtractAndInsertVerbs(EntityUid uid, PlanktonTankComponent component, GetVerbsEvent<Verb> args)
    {
        // Basic checks first, like if we can access or can interact.
        // We also check whether the Tank has a Plankton component,
        // and if it has a container component.
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<PlanktonComponent>(uid, out var plankton))
            return;


        if (!_container.TryGetContainer(uid, "plankton_container_slot", out var slot)
            || slot.ContainedEntities.Count == 0)
            return;

        // Now we check if the FIRST slot of the container is also a Plankton component.
        // If it is and species count IS ZERO, we add a verb to EXTRACT species from the tank.
        // Otherwise, if species is NOT zero, we insert the first one in the container.
        var containerEntity = slot.ContainedEntities[0];
        if (!TryComp<PlanktonComponent>(containerEntity, out var containerPlankton))
            return;

        var livingSpecies = plankton.SpeciesInstances.ToList();
        foreach (var species in livingSpecies)
        {
            var capturedSpecies = species;

            Verb extractVerb = new()
            {
                Text = Loc.GetString("plankton-tank-extract-species",
                    ("species", capturedSpecies.SpeciesName.ToString())),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    ExtractSpecies(uid, component, capturedSpecies);
                },
                Priority = -1
            };

            args.Verbs.Add(extractVerb);
        }

        if (containerPlankton.SpeciesInstances.Count > 0)
        {
            var planktonSpecies = containerPlankton.SpeciesInstances.First();
            if (!planktonSpecies.IsAlive)
                return;

            Verb insertVerb = new()
            {
                Text = Loc.GetString("plankton-tank-insert-species",
                    ("species", planktonSpecies.SpeciesName.ToString())),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () =>
                {
                    InsertSpecies(uid, component, containerPlankton);
                },
                Priority = -2,
            };

            args.Verbs.Add(insertVerb);
        }
    }

    /// <summary>
    ///     Verb method for inserting species into the tank.
    /// </summary>
    /// <param name="tankUid">The Tank UID</param>
    /// <param name="tankComponent">The Tank Component</param>
    /// <param name="species">The Species Component</param>
    private void InsertSpecies(EntityUid tankUid, PlanktonTankComponent tankComponent, PlanktonComponent species)
    {
        // If we can't add a species because the tank is full, we return early.
        // We also check if the tank can even contain plankton.
        if (!CanAddSpecies(tankUid, tankComponent))
        {
            _popup.PopupEntity(Loc.GetString("plankton-tank-full"), tankUid);
            return;
        }

        if (!TryComp<PlanktonComponent>(tankUid, out var tankPlankton))
            return;

        // If we pass those checks, we then get the first species in the container.
        // We then add it to the tank, and remove it from the container.
        // Finally, we display a message at the tank.
        var firstPlanktonSpecies = species.SpeciesInstances.First();

        tankPlankton.SpeciesInstances.Add(firstPlanktonSpecies);
        species.SpeciesInstances.Remove(firstPlanktonSpecies);

        _popup.PopupEntity(Loc.GetString("plankton-tank-inserted", ("species", firstPlanktonSpecies.SpeciesName.ToString())), tankUid);
    }

    /// <summary>
    ///     Verb method for extracting species from the tank, and into a container.
    /// </summary>
    /// <param name="tankUid">The Tank UID</param>
    /// <param name="tankComponent">The Tank Component</param>
    /// <param name="species">The Plankton Species Instance</param>
    private void ExtractSpecies(EntityUid tankUid,
        PlanktonTankComponent tankComponent,
        PlanktonComponent.PlanktonSpeciesInstance species)
    {
        // Get the tank's plankton component,
        // and then the container slot and the entity inside of it.
        if (!TryComp<PlanktonComponent>(tankUid, out var tankPlankton))
            return;

        if (!_container.TryGetContainer(tankUid, "plankton_container_slot", out var slot) ||
            slot.ContainedEntities.Count == 0)
            return;

        var containerEntity = slot.ContainedEntities[0];
        if (!TryComp<PlanktonComponent>(containerEntity, out var containerPlankton))
            return;

        // Now we add the species to the container,
        // remove it from the tank,
        // and do client-side stuff like audio and a message.
        containerPlankton.SpeciesInstances.Add(species);
        tankPlankton.SpeciesInstances.Remove(species);

        _audio.PlayPvs(tankComponent.ExtractSound, tankUid);
        _popup.PopupEntity(Loc.GetString("plankton-tank-extracted", ("species", species.SpeciesName.ToString())), tankUid);
    }

    /// <summary>
    ///     Checks if the tank can accept another species.
    /// </summary>
    private bool CanAddSpecies(EntityUid uid, PlanktonTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!TryComp<PlanktonComponent>(uid, out var plankton))
            return false;

        return plankton.SpeciesInstances.Count < component.MaxSpecies;
    }
}
