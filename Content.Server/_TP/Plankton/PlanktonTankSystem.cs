using System.Linq;
using Content.Server.Power.Components;
using Content.Shared._TP.Plankton;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Radiation.Components;
using Content.Shared.Speech;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server._TP.Plankton;

/// <summary>
///     Handles the planktology tank.
/// </summary>
public sealed partial class PlanktonTankSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    private const float UpdateInterval = 1f;
    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanktonTankComponent, ComponentInit>(OnTankInit);
        SubscribeLocalEvent<PlanktonTankComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<PlanktonTankComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlanktonTankComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<PlanktonTankComponent, GetVerbsEvent<Verb>>(AddNormalVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateInterval)
        {
            var query = EntityQueryEnumerator<PlanktonTankComponent>();
            while (query.MoveNext(out var tankUid, out var tankComp))
            {
                CheckTankSystems(tankUid);
                CheckPlanktonSurvival(tankUid, tankComp);
            }

            _updateTimer = 0f;
        }
    }

    private void CheckTankSystems(EntityUid tankUid)
    {
        if (!TryComp<PlanktonComponent>(tankUid, out var plankton))
            return;

        foreach (var species in plankton.SpeciesInstances.Where(species => species.IsAlive))
        {
            if ((species.Characteristics & PlanktonComponent.PlanktonCharacteristics.MagneticField) != 0)
            {
                if (TryComp<ApcPowerReceiverComponent>(tankUid, out var receiver) && receiver.Powered)
                    receiver.Load = 1000;
            }
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
        component.LightEnabled = args.Powered;
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
        if (_solution.TryGetSolution(uid, tank.WaterSolutionName, out _, out var solution))
        {
            var seawaterAmount = 0f;

            foreach (var reagent in solution.Contents)
            {
                if (reagent.Reagent.Prototype == "SeaWater")
                {
                    seawaterAmount += reagent.Quantity.Float();
                    solution.RemoveReagent("SeaWater", reagent.Quantity.Float());
                }
            }

            if (seawaterAmount < tank.MinimumSeawaterVolume)
            {
                foreach (var species in plankton.SpeciesInstances)
                {
                    HurtColony(uid, tank, species, plankton, 2F);
                }

                return;
            }
        }

        // Check the temperature tolerance for each species of plankton in the tank.
        foreach (var species in plankton.SpeciesInstances)
        {
            if (!species.IsAlive)
                continue;

            if ((species.Characteristics & PlanktonComponent.PlanktonCharacteristics.Cryophilic) != 0)
            {
                if (tank.CurrentTemperature != 0)
                {
                    HurtColony(uid, tank, species, plankton, 1F);
                }
            }
            else if ((species.Characteristics & PlanktonComponent.PlanktonCharacteristics.Pyrophilic) != 0)
            {
                if (tank.CurrentTemperature != 2)
                {
                    HurtColony(uid, tank, species, plankton, 1F);
                }
            }
            else
            {
                if (tank.CurrentTemperature != 1)
                {
                    HurtColony(uid, tank, species, plankton, 1F);
                }
            }
        }
    }

    private void HurtColony(EntityUid tankUid,
        PlanktonTankComponent tankComp,
        PlanktonComponent.PlanktonSpeciesInstance species,
        PlanktonComponent plankton,
        float killSize)
    {
        if (!species.IsAlive)
            return;

        var aks = Math.Min(species.CurrentSize, killSize);
        species.CurrentSize -= aks;
        plankton.DeadPlankton += aks;

        if (species.CurrentSize <= 0)
        {
            species.IsAlive = false;

            RemComp<PointLightComponent>(tankUid);
            RemComp<ElectrifiedComponent>(tankUid);
            RemComp<RadiationSourceComponent>(tankUid);

            if (TryComp<ApcPowerReceiverComponent>(tankUid, out var receiver))
                receiver.Load = tankComp.IdlePowerConsumption;

            Log.Debug($"The colony of plankton: {species.SpeciesName} has died.");
        }
    }

    private void OnExamined(EntityUid uid, PlanktonTankComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver) && receiver.Powered)
            args.PushMarkup(Loc.GetString("$tank-examine-power",  ("power", receiver.Load.ToString("F2"))));

        args.PushMarkup(Loc.GetString($"tank-examine-temp-{component.CurrentTemperature}"));

        if (TryComp<PlanktonComponent>(uid, out var plankton))
        {
            var speciesCount = plankton.SpeciesInstances.Count;
            var deadCount = plankton.DeadPlankton;

            if (deadCount > 300)
                speciesCount += 1;

            args.PushMarkup(Loc.GetString("tank-examine-colony-count",
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
    private void AddAlternativeVerbs(EntityUid uid, PlanktonTankComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        // Basic checks first, like if we can access or can interact.
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!component.IsPowered)
            return;

        var newTemp = component.CurrentTemperature switch
        {
            0 => 1,
            1 => 2,
            2 => 0,
            _ => 1,
        };

        // Increase temperature
        AlternativeVerb changeTemp = new()
        {
            Text = Loc.GetString("plankton-tank-increase-temp"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () =>
            {
                component.CurrentTemperature = newTemp;
                _audio.PlayPvs(component.AdjustSound, uid);
                _popup.PopupEntity(Loc.GetString("plankton-tank-temp-increased", ("temp", $"{newTemp:F1}")), uid, args.User);
            },
            Priority = 1
        };

        args.Verbs.Add(changeTemp);
    }

    /// <summary>
    ///     Verbs for extracting and inserting plankton colonies.
    /// </summary>
    /// <param name="uid">The Tank UID</param>
    /// <param name="tankComponent">The Tank Component</param>
    /// <param name="args">GetVerbsEvent for Verb arguments</param>
    private void AddNormalVerbs(EntityUid uid, PlanktonTankComponent tankComponent, GetVerbsEvent<Verb> args)
    {
        // Basic checks first, like if we can access or can interact.
        // We also check whether the Tank has a Plankton component,
        // and if it has a container component.
        if (!args.CanAccess || !args.CanInteract)
            return;

        Verb toggleLightVerb = new()
        {
            Text = Loc.GetString("plankton-verb-tank-toggle-light"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () =>
            {
                _popup.PopupEntity(tankComponent.LightEnabled
                    ? Loc.GetString("tank-popup-light-off")
                    : Loc.GetString("tank-popup-light-on"),
                    uid,
                    PopupType.Medium);

                tankComponent.LightEnabled = !tankComponent.LightEnabled;
            },
            Priority = 0,
        };
        args.Verbs.Add(toggleLightVerb);


        if (!TryComp<PlanktonComponent>(uid, out var planktonComp))
            return;

        if (!_container.TryGetContainer(uid, "plankton_container_slot", out var slot)
            || slot.ContainedEntities.Count == 0)
            return;

        if (planktonComp.SpeciesInstances.Count != 0)
        {
            Verb extractVerb = new()
            {
                Text = Loc.GetString("tank-verb-extract-species"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () => ExtractSpecies(uid, tankComponent, planktonComp),
                Priority = -1
            };
            args.Verbs.Add(extractVerb);
        }

        if (TryComp<PlanktonComponent>(slot.ContainedEntities[0], out var slotPlankton)
            && slotPlankton.SpeciesInstances.Count != 0)
        {
            Verb insertVerb = new()
            {
                Text = Loc.GetString("tank-verb-insert-species"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () =>
                {
                    InsertSpecies(uid, tankComponent, planktonComp);
                },
                Priority = -2,
            };
            args.Verbs.Add(insertVerb);
        }
    }

    /// <summary>
    ///     Verb method for inserting species into the tank.
    /// </summary>
    /// <param name="tankUid">The Tank uid</param>
    /// <param name="tankComponent">The Tank component</param>
    /// <param name="planktonComp">The Plankton component</param>
    private void InsertSpecies(EntityUid tankUid, PlanktonTankComponent tankComponent, PlanktonComponent planktonComp)
    {
        // If we can't add a species because the tank is full, we return early.
        // We also check if the tank can even contain plankton.
        if (!CanAddSpecies(tankUid, tankComponent))
        {
            _popup.PopupEntity(Loc.GetString("plankton-tank-full"), tankUid);
            return;
        }

        if (!_container.TryGetContainer(tankUid, "plankton_container_slot", out var slot) ||
            slot.ContainedEntities.Count == 0)
            return;

        var containerEntity = slot.ContainedEntities[0];
        if (!TryComp<PlanktonComponent>(containerEntity, out var containerPlankton))
            return;


        // If we pass those checks, we then get every species in the container.
        // We then add them to the tank while removing them from the container.
        // Finally, we display a message at the tank.
        var capturedSpecies = containerPlankton.SpeciesInstances.ToList();
        foreach (var species in capturedSpecies)
        {
            if (planktonComp.SpeciesInstances.Count + 1 > tankComponent.MaxSpecies)
                continue;

            planktonComp.SpeciesInstances.Add(species);
            containerPlankton.SpeciesInstances.Remove(species);
        }

        _audio.PlayPvs(tankComponent.ExtractSound, tankUid);
        _popup.PopupEntity(Loc.GetString("plankton-tank-inserted"), tankUid);

        if (TryComp<MindContainerComponent>(containerEntity, out var mindContainer))
            MoveMind(mindContainer, tankUid, containerEntity);
    }

    /// <summary>
    ///     Verb method for extracting species from the tank, and into a container.
    /// </summary>
    /// <param name="tankUid">The Tank uid</param>
    /// <param name="tankComponent">The Tank component</param>
    /// <param name="planktonComp">The Plankton component</param>
    private void ExtractSpecies(EntityUid tankUid, PlanktonTankComponent tankComponent, PlanktonComponent planktonComp)
    {
        if (!_container.TryGetContainer(tankUid, "plankton_container_slot", out var slot) ||
            slot.ContainedEntities.Count == 0)
            return;

        var containerEntity = slot.ContainedEntities[0];
        if (!TryComp<PlanktonComponent>(containerEntity, out var containerPlankton))
            return;

        // Now we add the species to the container,
        // remove it from the tank,
        // and do client-side stuff like audio and a message.

        var capturedSpecies = planktonComp.SpeciesInstances.ToList();
        foreach (var species in capturedSpecies)
        {
            containerPlankton.SpeciesInstances.Add(species);
            planktonComp.SpeciesInstances.Remove(species);
        }

        _audio.PlayPvs(tankComponent.ExtractSound, tankUid);
        _popup.PopupEntity(Loc.GetString("plankton-tank-extracted"), tankUid);

        if (TryComp<MindContainerComponent>(tankUid, out var mindContainer))
            MoveMind(mindContainer, containerEntity, tankUid);

        if (TryComp<ApcPowerReceiverComponent>(tankUid, out var powerReceiver))
            powerReceiver.Load = tankComponent.IdlePowerConsumption;
    }

    private void MoveMind(MindContainerComponent mindCont, EntityUid containerEnt, EntityUid movedUid)
    {
        if (mindCont.Mind == null)
            return;

        _mind.TransferTo(mindCont.Mind.Value, containerEnt);
        EnsureComp<SpeechComponent>(containerEnt);
        EnsureComp<TypingIndicatorComponent>(containerEnt);

        RemComp<PointLightComponent>(movedUid);
        RemComp<ElectrifiedComponent>(movedUid);
        RemComp<RadiationSourceComponent>(movedUid);
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

        var belowMax = plankton.SpeciesInstances.Count < component.MaxSpecies;
        var belowMaxDead = plankton.DeadPlankton <= 300;

        return belowMax && belowMaxDead;
    }
}
