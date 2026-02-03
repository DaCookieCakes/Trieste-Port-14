using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared._TP.Plankton;
using Content.Shared.Atmos;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
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
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
                harvesterComp.CanHarvest = containerSlot.ContainedEntities.Count > 0
                                           && harvesterComp.NextCooldown <= _timing.CurTime;

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
    }

    /// <summary>
    ///     Examination messages for the Harvester entity
    /// </summary>
    /// <param name="uid">Harvester Uid</param>
    /// <param name="component">Harvester Component</param>
    /// <param name="args">ExaminedEvent arguments</param>
    private void OnExamined(EntityUid uid, PlanktonHarvesterComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // Power examination
        var powerStatus = component.IsPowered ? "online" : "offline";
        args.PushMarkup(Loc.GetString("plankton-harvester-examine-power", ("status", powerStatus)));

        // Issue and Harvesting examination
        // Issues include the container being empty, and the harvester cooldown.
        // Harvesting is just a timer when it CAN harvest.
        var container = _container.GetContainer(uid, component.ContainerSlotId);
        var cooldown = (component.NextCooldown - _timing.CurTime).TotalSeconds;
        if (!component.CanHarvest)
        {
            if (container.ContainedEntities.Count <= 0)
                args.PushMarkup("plankton-harvester-container-empty");

            args.PushMarkup(component.NextCooldown > _timing.CurTime
                ? Loc.GetString("plankton-harvester-cooldown", ("time", $"{cooldown:F0}"))
                : Loc.GetString("plankton-harvester-ready"));
        }
        else
        {
            var harvestTime = (component.NextHarvestTime - _timing.CurTime).TotalSeconds;
            if (component.CanHarvest)
                args.PushMarkup(Loc.GetString("plankton-harvester-harvesting", ("time", $"{harvestTime:F0}")));
        }
    }

    private void OnInteractUsing(EntityUid uid, PlanktonHarvesterComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Check if the used item is a valid Plankton container,
        // and whether the slot is already occupied.
        if (!HasComp<PlanktonComponent>(args.Used))
            return;

        var container = _container.GetContainer(uid, component.ContainerSlotId);

        // If valid and the slot isn't occupied, insert the container.
        if (_container.Insert(args.Used, container))
        {
            _popup.PopupEntity(Loc.GetString("plankton-harvester-container-inserted"), uid, args.User);
            args.Handled = true;
        }
    }

    private void OnContainerInserted(EntityUid uid, PlanktonHarvesterComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.ContainerSlotId)
            return;

        // Update the power consumption when a container is inserted.
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
        {
            receiver.Load = component.IdlePowerConsumption;
        }
    }

    private void OnContainerRemoved(EntityUid uid, PlanktonHarvesterComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.ContainerSlotId)
            return;

        component.CanHarvest = false;
    }

    private void TryHarvestPlankton(EntityUid uid, PlanktonHarvesterComponent component)
    {
        // Check if we have a container loaded, and set the next harvest time 30 seconds from now.
        var container = _container.GetContainer(uid, component.ContainerSlotId);
        if (container.ContainedEntities.Count == 0)
        {
            component.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(component.HarvestInterval);
            return;
        }

        var containerEntity = container.ContainedEntities[0];
        if (!TryComp<PlanktonComponent>(containerEntity, out var plankton))
        {
            component.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(component.HarvestInterval);
            return;
        }

        if (plankton.SpeciesInstances.Count >= 2)
        {
            return;
        }

        // Get the pipe node and check the air mixture.
        // If it's SeaWater, consume it from the pipe and start harvesting plankton.
        if (!_nodeContainer.TryGetNode(uid, "pipe", out PipeNode? pipeNode))
        {
            Log.Error($"PlanktonHarvester {uid} has no pipe node!");
            component.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(component.HarvestInterval);
            return;
        }

        var airMixture = pipeNode.Air;
        if (airMixture.TotalMoles == 0)
        {
            FailHarvest(uid, component, "plankton-harvester-no-atmosphere");
            return;
        }

        var seawaterAmount = airMixture.GetMoles(Gas.Water);
        if (seawaterAmount < component.SeaWaterRequired)
        {
            FailHarvest(uid, component, "plankton-harvester-insufficient-seawater");
            return;
        }

        airMixture.AdjustMoles(Gas.Water, -component.SeaWaterRequired);

        component.CanHarvest = true;

        // Once we're harvesting,we set the power load to the ACTIVE amount.
        // We then generate the plankton species, and run the complete harvest method.
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            receiver.Load = component.ActivePowerConsumption;

        var speciesCount = _random.Next(component.MinSpecies, component.MaxSpecies + 1);
        GeneratePlanktonSpecies(containerEntity, plankton, speciesCount);
        CompleteHarvest(uid, component);
    }

    private void GeneratePlanktonSpecies(EntityUid containerUid, PlanktonComponent plankton, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var firstName = PlanktonComponent.PlanktonFirstNames[
                _random.Next(PlanktonComponent.PlanktonFirstNames.Length)];

            var secondName = PlanktonComponent.PlanktonSecondNames[
                _random.Next(PlanktonComponent.PlanktonSecondNames.Length)];

            var planktonName = new PlanktonComponent.PlanktonName(firstName, secondName);

            // Randomly generate 2-3 characteristics
            var numCharacteristics = _random.Next(2, 4);
            var possibleCharacteristics = Enum.GetValues<PlanktonComponent.PlanktonCharacteristics>();
            var selectedCharacteristics = new HashSet<PlanktonComponent.PlanktonCharacteristics>();

            while (selectedCharacteristics.Count < numCharacteristics)
            {
                var characteristicValue = possibleCharacteristics.GetValue(_random.Next(possibleCharacteristics.Length));
                if (characteristicValue != null)
                {
                    var randomCharacteristic = (PlanktonComponent.PlanktonCharacteristics)characteristicValue;

                    // Prevent Cryophilic + Pyrophilic
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

            // Combine characteristics
            PlanktonComponent.PlanktonCharacteristics combinedCharacteristics = 0;
            foreach (var characteristic in selectedCharacteristics)
            {
                combinedCharacteristics |= characteristic;
            }

            // Create new plankton instance
            var planktonInstance = new PlanktonComponent.PlanktonSpeciesInstance(
                planktonName,
                (PlanktonComponent.PlanktonDiet)_random.Next(
                    Enum.GetValues<PlanktonComponent.PlanktonDiet>().Length),
                combinedCharacteristics,
                1.0f,
                50f,
                true
            );

            plankton.SpeciesInstances.Add(planktonInstance);

            Log.Info($"Harvested plankton species {planktonInstance.SpeciesName} with diet {planktonInstance.Diet} and characteristics {combinedCharacteristics}");
        }
    }

    private void CompleteHarvest(EntityUid uid, PlanktonHarvesterComponent component)
    {
        component.CanHarvest = false;
        component.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(component.HarvestInterval);

        _audio.PlayPvs(component.HarvestSound, uid);
        _popup.PopupEntity(Loc.GetString("plankton-harvester-success"), uid);

        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            receiver.Load = component.IdlePowerConsumption;
    }

    private void FailHarvest(EntityUid uid, PlanktonHarvesterComponent component, string messageKey)
    {
        // Retries have a shorter timer.
        component.NextHarvestTime = _timing.CurTime + TimeSpan.FromSeconds(component.HarvestInterval / 2);

        _audio.PlayPvs(component.FailSound, uid);
        _popup.PopupEntity(Loc.GetString(messageKey), uid);
    }
}
