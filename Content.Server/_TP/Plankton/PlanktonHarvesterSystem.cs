using System.Linq;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared._TP.Plankton;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._TP.Plankton;

/// <summary>
///     Handles plankton harvesting from SeaWater through atmospheric pipes
/// </summary>
public sealed class PlanktonHarvesterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanktonHarvesterComponent, ComponentInit>(OnHarvesterInit);
        SubscribeLocalEvent<PlanktonHarvesterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<PlanktonHarvesterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlanktonHarvesterComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlanktonHarvesterComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<PlanktonHarvesterComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
    }

    private float _updateTimer;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer >= 1F)
        {
            var harvesterQuery = EntityQueryEnumerator<PlanktonHarvesterComponent>();
            while (harvesterQuery.MoveNext(out var harvesterUid, out var harvesterComp))
            {
                if (!harvesterComp.IsPowered)
                    continue;

                var containerSlot = _container.GetContainer(harvesterUid, harvesterComp.ContainerSlotId);

                if (containerSlot.ContainedEntities.Count <= 0)
                    continue;

                harvesterComp.CanHarvest = containerSlot.ContainedEntities.Count > 0
                                           && harvesterComp.NextCooldown <= _timing.CurTime
                                           && HasComp<PlanktonComponent>(containerSlot.ContainedEntities[0]);

                if (harvesterComp.CanHarvest)
                {
                    TryHarvestPlankton(harvesterUid, harvesterComp);
                }
                else
                {
                    harvesterComp.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.HarvestInterval);
                }
            }

            _updateTimer = 0F;
        }
    }

    /// <summary>
    ///     Initialization for the Harvester entity
    ///     <para>This ensures a container slot, whether it's powered, and sets the next harvest time.</para>
    /// </summary>
    /// <param name="harvesterUid">Harvester Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    /// <param name="args">ComponentInit arguments</param>
    private void OnHarvesterInit(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp, ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(harvesterUid, harvesterComp.ContainerSlotId);

        harvesterComp.IsPowered = TryComp<ApcPowerReceiverComponent>(harvesterUid, out var receiver) && receiver.Powered;
        harvesterComp.NextCooldown = TimeSpan.Zero;
        harvesterComp.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.HarvestInterval);
    }

    /// <summary>
    ///     Toggles the power and harvesting
    /// </summary>
    /// <param name="harvesterUid">Harvetser Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    /// <param name="args">PowerChargedEvent arguments</param>
    private void OnPowerChanged(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp, ref PowerChangedEvent args)
    {
        harvesterComp.IsPowered = args.Powered;
        harvesterComp.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.HarvestInterval);

        if (!harvesterComp.IsPowered)
            harvesterComp.NextCooldown = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.CooldownInterval);
    }

    /// <summary>
    ///     Examination messages for the Harvester entity
    /// </summary>
    /// <param name="harvesterUid">Harvester Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    /// <param name="args">ExaminedEvent arguments</param>
    private void OnExamined(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // Power examination
        var powerStatus = harvesterComp.IsPowered ? "online" : "offline";
        args.PushMarkup(Loc.GetString("plankton-harvester-examine-power", ("status", powerStatus)));

        // Issue and Harvesting examination
        // Issues include the container being empty, and the harvester cooldown.
        // Harvesting is just a timer when it CAN harvest.
        var container = _container.GetContainer(harvesterUid, harvesterComp.ContainerSlotId);
        var cooldown = (harvesterComp.NextCooldown - _timing.CurTime).TotalSeconds;
        if (!harvesterComp.CanHarvest)
        {
            if (container.ContainedEntities.Count <= 0)
                args.PushMarkup(Loc.GetString("plankton-harvester-examine-container-empty"));

            args.PushMarkup(harvesterComp.NextCooldown > _timing.CurTime
                ? Loc.GetString("plankton-harvester-examine-cooldown", ("time", $"{cooldown:F0}"))
                : Loc.GetString("plankton-harvester-examine-ready"));
        }
        else
        {
            var harvestTime = (harvesterComp.NextHarvestTime - _timing.CurTime).TotalSeconds;
            if (harvesterComp.CanHarvest)
                args.PushMarkup(Loc.GetString("plankton-harvester-examine-harvesting", ("time", $"{harvestTime:F0}")));
        }
    }

    /// <summary>
    ///     Inserting am item on the Harvester.
    /// </summary>
    /// <param name="harvesterUid">Harvester Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    /// <param name="args">InteractUsingEvent arguments</param>
    private void OnInteractUsing(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Check if the used item is a valid Plankton container,
        // and if valid insert it into the harvester.
        if (!HasComp<PlanktonComponent>(args.Used))
            return;

        var container = _container.GetContainer(harvesterUid, harvesterComp.ContainerSlotId);
        if (_container.Insert(args.Used, container))
        {
            _popup.PopupEntity(Loc.GetString("plankton-harvester-container-inserted"), harvesterUid, args.User);
            args.Handled = true;
        }
    }

    /// <summary>
    ///     After an item is inserted into the harvester.
    /// </summary>
    /// <param name="harvesterUid">Harvester Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    /// <param name="args">EntInsertedIntoContainerMessage arguments</param>
    private void OnContainerInserted(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != harvesterComp.ContainerSlotId)
            return;

        // Update the power consumption when a container is inserted
        if (TryComp<ApcPowerReceiverComponent>(harvesterUid, out var receiver) && harvesterComp.CanHarvest)
            receiver.Load = harvesterComp.ActivePowerConsumption;

        harvesterComp.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.HarvestInterval);
    }

    /// <summary>
    ///     After an item is removed from the harvester.
    /// </summary>
    /// <param name="harvesterUid">Harvester Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    /// <param name="args">EntRemovedFromContainerMessage arguments</param>
    private void OnContainerRemoved(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != harvesterComp.ContainerSlotId)
            return;

        // Reset the harvest time
        harvesterComp.CanHarvest = false;
    }

    /// <summary>
    ///     Tries to harvest plankton
    /// </summary>
    /// <param name="harvesterUid">Harvester Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    private void TryHarvestPlankton(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp)
    {
        // Check if we have a container loaded, and then check if it's an instance of Plankton.
        // If so, we check if it's above the max species this harvester can generate.
        // Then we move onto pipe nodes.
        var container = _container.GetContainer(harvesterUid, harvesterComp.ContainerSlotId);

        var containerEntity = container.ContainedEntities[0];
        if (!TryComp<PlanktonComponent>(containerEntity, out var plankton))
            return;

        if (plankton.SpeciesInstances.Count >= harvesterComp.MaxSpecies)
            return;

        // Finally we generate the species. The count is based on the harvester itself.
        if (harvesterComp.NextHarvestTime < _timing.CurTime)
        {
            // If the harvester needs water, we get the pipe node and check the air mixture.
            // If there IS seawater, consume it from the pipe.
            // If there IS NO seawater, fail the harvest.
            if (harvesterComp.SeaWaterRequired > 0)
            {
                if (!_nodeContainer.TryGetNode(harvesterUid, "pipe", out PipeNode? pipeNode))
                    return;

                var airMixture = pipeNode.Air;
                if (airMixture.TotalMoles == 0)
                {
                    _popup.PopupEntity(Loc.GetString("plankton-harvester-no-atmosphere"), harvesterUid);
                    _audio.PlayPvs(harvesterComp.FailSound, harvesterUid, AudioParams.Default.WithVolume(0.6F));
                    harvesterComp.CanHarvest = false;
                    harvesterComp.NextCooldown = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.CooldownInterval);
                    return;
                }

                var seawaterAmount = airMixture.GetMoles(Gas.Water);
                if (seawaterAmount < harvesterComp.SeaWaterRequired)
                {
                    _popup.PopupClient(Loc.GetString("plankton-harvester-insufficient-seawater"), harvesterUid);
                    _audio.PlayPvs(harvesterComp.FailSound, harvesterUid, AudioParams.Default.WithVolume(0.6F));
                    harvesterComp.CanHarvest = false;
                    harvesterComp.NextCooldown = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.CooldownInterval);
                    return;
                }

                airMixture.AdjustMoles(Gas.Water, -harvesterComp.SeaWaterRequired);
            }

            var speciesCount = _random.Next(harvesterComp.MinSpecies, harvesterComp.MaxSpecies);
            GeneratePlanktonSpecies(plankton, speciesCount);
            CompleteHarvest(harvesterUid, harvesterComp);
        }
    }

    /// <summary>
    ///     Generates an entirely new Plankton species with three characteristics.
    /// </summary>
    /// <param name="planktonComp">Plankton Component</param>
    /// <param name="speciesCount">How many species to generate</param>
    private void GeneratePlanktonSpecies(PlanktonComponent planktonComp, int speciesCount)
    {
        for (var i = 0; i < speciesCount; i++)
        {
            var firstName = PlanktonComponent.PlanktonFirstNames[
                _random.Next(PlanktonComponent.PlanktonFirstNames.Length)];

            var secondName = PlanktonComponent.PlanktonSecondNames[
                _random.Next(PlanktonComponent.PlanktonSecondNames.Length)];

            var planktonName = new PlanktonComponent.PlanktonName(firstName, secondName);

            // Randomly generate 3 characteristics.
            // TODO - Weighted pool for characteristics/diets that fit together
            // TODO - Pool for incompatible characteristics
            const int numCharacteristics = 3;
            var possibleCharacteristics = Enum.GetValues<PlanktonComponent.PlanktonCharacteristics>();
            var selectedCharacteristics = new HashSet<PlanktonComponent.PlanktonCharacteristics>();

            while (selectedCharacteristics.Count < numCharacteristics)
            {
                var characteristicValue = possibleCharacteristics.GetValue(_random.Next(possibleCharacteristics.Length));
                if (characteristicValue != null)
                {
                    var randomCharacteristic = (PlanktonComponent.PlanktonCharacteristics)characteristicValue;

                    // TEMP - Prevent Cryophilic + Pyrophilic
                    if ((selectedCharacteristics.Contains(PlanktonComponent.PlanktonCharacteristics.Cryophilic) &&
                         randomCharacteristic == PlanktonComponent.PlanktonCharacteristics.Pyrophilic) ||
                        (selectedCharacteristics.Contains(PlanktonComponent.PlanktonCharacteristics.Pyrophilic) &&
                         randomCharacteristic == PlanktonComponent.PlanktonCharacteristics.Cryophilic))
                    {
                        continue;
                    }

                    selectedCharacteristics.Add(randomCharacteristic);
                }
            }

            // Combine the characteristics, and then we create a new plankton species.
            PlanktonComponent.PlanktonCharacteristics combinedCharacteristics = 0;
            foreach (var characteristic in selectedCharacteristics)
            {
                combinedCharacteristics |= characteristic;
            }
            var diet = PickValidDiet(combinedCharacteristics);

            var planktonInstance = new PlanktonComponent.PlanktonSpeciesInstance(
                planktonName,
                (PlanktonComponent.PlanktonDiet)_random.Next(
                    Enum.GetValues<PlanktonComponent.PlanktonDiet>().Length),
                combinedCharacteristics,
                25f,
                50f,
                true
            );

            if (planktonInstance is { Diet: PlanktonComponent.PlanktonDiet.Chemophage, PreferredReagent: null })
            {
                var reagentString = _random.Pick((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0
                        ? PlanktonComponent.ChemophageExoticReagents
                        : PlanktonComponent.ChemophageReagents);

                var reagentId = new ReagentId(reagentString, null);
                planktonInstance.PreferredReagent = reagentId;

                Log.Info($"{planktonInstance.SpeciesName} is a Chemophage that feeds on {planktonInstance.PreferredReagent}");
            }

            planktonComp.SpeciesInstances.Add(planktonInstance);

            Log.Info($"Harvested plankton species {planktonInstance.SpeciesName} with diet {planktonInstance.Diet} and characteristics {combinedCharacteristics}");
        }
    }

    private object PickValidDiet(PlanktonComponent.PlanktonCharacteristics characteristics)
    {
        var allDiets = Enum.GetValues<PlanktonComponent.PlanktonDiet>().ToList();
        var validDiets = new List<PlanktonComponent.PlanktonDiet>(allDiets);

        if ((characteristics & PlanktonComponent.PlanktonCharacteristics.Radioactive) != 0)
            validDiets.Remove(PlanktonComponent.PlanktonDiet.Radiophage);

        if ((characteristics & PlanktonComponent.PlanktonCharacteristics.ChemicalProduction) != 0)
            validDiets.Remove(PlanktonComponent.PlanktonDiet.Chemophage);

        return validDiets.Count == 0 ? PlanktonComponent.PlanktonDiet.Scavenger : _random.Pick(validDiets);
    }

    /// <summary>
    ///     Completes the harvest once finished generating species
    /// </summary>
    /// <param name="harvesterUid">Harvester Uid</param>
    /// <param name="harvesterComp">Harvester Component</param>
    private void CompleteHarvest(EntityUid harvesterUid, PlanktonHarvesterComponent harvesterComp)
    {
        harvesterComp.CanHarvest = false;
        harvesterComp.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.HarvestInterval);
        harvesterComp.NextCooldown = _timing.CurTime + TimeSpan.FromSeconds(harvesterComp.CooldownInterval);

        _audio.PlayPvs(harvesterComp.HarvestSound, harvesterUid, AudioParams.Default.WithVolume(0.6F));
        _popup.PopupEntity(Loc.GetString("plankton-harvester-success"), harvesterUid);

        if (TryComp<ApcPowerReceiverComponent>(harvesterUid, out var receiver))
            receiver.Load = harvesterComp.IdlePowerConsumption;
    }
}
