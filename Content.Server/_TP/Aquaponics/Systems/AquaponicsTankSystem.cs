using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared._TP.Aquaponics.Components;
using Content.Shared._TP.Aquaponics.Data;
using Content.Shared._TP.Aquaponics.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._TP.Aquaponics.Systems;

/// <summary>
///     The system that handles the aquaponics tank components.
/// </summary>
public sealed class AquaponicsTankSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private readonly Dictionary<FishData, float> _agingMultipliers = new();

    private static readonly ProtoId<TagPrototype> ScoopTag = "TP14TagScoop";
    private static readonly Dictionary<FishGrowthStage, AquaponicsTankVisuals> FishVisuals = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AquaponicsTankComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AquaponicsTankComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AquaponicsTankComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AquaponicsTankComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<AquaponicsTankComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntRemoved(Entity<AquaponicsTankComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        _appearance.SetData(ent.Owner, AquaponicsTankVisuals.Beaker, false);
    }

    private void OnEntInserted(Entity<AquaponicsTankComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _appearance.SetData(ent.Owner, AquaponicsTankVisuals.Beaker, true);
    }

    private void OnExamined(Entity<AquaponicsTankComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, "waste", out _, out var wasteSol))
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, "nutrients", out _, out var foodSol))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, "beaker", out var tankSlot))
            return;

        if (!tankSlot.HasItem)
            args.PushMarkup(Loc.GetString("aquaponics-tank-examine-no-beaker"), 1);

        if (tankSlot is {HasItem: true, Item: not null})
        {
            if (_solutionContainer.TryGetSolution(tankSlot.Item.Value, "beaker", out _, out var beakerSol))
            {
                args.PushMarkup(Loc.GetString("aquaponics-tank-examine-beaker-contents",
                        ("volume", beakerSol.Volume),
                        ("maxVolume", beakerSol.MaxVolume)),
                    1);
            }
        }

        args.PushMarkup(Loc.GetString("aquaponics-tank-examine-nutrients", ("food", foodSol.Volume)), 2);
        args.PushMarkup(Loc.GetString("aquaponics-tank-examine-waste", ("waste", wasteSol.Volume)), 3);

        if (ent.Comp.DiseaseLevel >= 40)
            args.PushMarkup(Loc.GetString("aquaponics-tank-examine-disease", ("disease", ent.Comp.DiseaseLevel)), 4);

        if (ent.Comp.CurrentFish.Count == 0)
            args.PushMarkup(Loc.GetString("aquaponics-tank-examine-no-fish"), 4);

        foreach (var fish in ent.Comp.CurrentFish)
        {
            var speciesId = fish.SpeciesId;
            if (!_proto.TryIndex<FishSpeciesPrototype>(speciesId, out var fishSpecies))
            {
                Log.Error($"Invalid or null fish species during examine event: {speciesId}");
                return;
            }

            args.PushMarkup(Loc.GetString("aquaponics-tank-examine-fish", ("species", fishSpecies.Name)), 5);

            args.PushMarkup(Loc.GetString("aquaponics-tank-examine-fish-growth", ("species", fishSpecies.Name), ("growth", fish.GrowthStage)), 6);
            args.PushMarkup(fish.IsDead
                    ? Loc.GetString("aquaponics-tank-examine-fish-dead", ("species", fishSpecies.Name))
                    : Loc.GetString("aquaponics-tank-examine-fish-health", ("species", fishSpecies.Name), ("health", fish.Health)),
                7);
        }
    }

    /// <summary>
    ///     An event to handle entity shutdown.
    ///     This is where we clean up to prevent memory leaks.
    /// </summary>
    /// <param name="ent">Aquaponics Tank Entity</param>
    /// <param name="args">ComponentShutdown arguments</param>
    private void OnShutdown(Entity<AquaponicsTankComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.CurrentFish.Clear();
        _agingMultipliers.Clear();
    }

    /// <summary>
    ///     An event to handle interacting with entities.
    ///     We use this for eggs and the scoop.
    /// </summary>
    /// <param name="ent">Aquaponics Tank Entity</param>
    /// <param name="args">InteractUsingEvent arguments</param>
    private void OnInteractUsing(Entity<AquaponicsTankComponent> ent, ref InteractUsingEvent args)
    {
        // If args are already handled, we return early.
        if (args.Handled)
            return;

        // Now a "tryComp" for the FishEgg component. We don't invert this one
        // because we want to make sure it's FOR eggs.
        if (TryComp<FishEggComponent>(args.Used, out var fishEggComp))
        {
            // If the tank is full, we pop up a message and return.
            if (ent.Comp.CurrentFish.Count >= ent.Comp.MaxFish)
            {
                _popup.PopupEntity(Loc.GetString("aquaponics-tank-message-full"), ent, args.User);
                args.Handled = true;
                return;
            }

            // Otherwise, we create a new fish from the egg's Species ID.
            // If we can't find it, we log an error.
            var speciesId = fishEggComp.SpeciesId;
            if (!_proto.TryIndex<FishSpeciesPrototype>(speciesId, out var fishSpecies))
            {
                Log.Error($"Invalid or null fish species during interact event: {speciesId}");
                return;
            }

            var newFish = CreateFishFromSpecies(fishSpecies);

            if (fishEggComp.Genes.Count > 0)
            {
                newFish.Genes = fishEggComp.Genes;
            }

            newFish.GeneticInstability = fishEggComp.GeneticInstability;
            newFish.Health -= newFish.GeneticInstability / 2;

            // Once the fish is created, we set the aging multiplier, add it to the tank,
            // and set some variables for the new data.
            // This includes genes, stability, and health.
            var agingMult = GetAgingTime(newFish);
            _agingMultipliers[newFish] = agingMult;
            ent.Comp.CurrentFish.Add(newFish);

            QueueDel(args.Used);
            args.Handled = true;
            return;
        }

        // If the used item has a "scoop" tag, we iterate through the tank's fish.
        // If any are adults or older, we add them to a list to be removed later.
        // Can't do it here for ConcurrentModification errors.
        if (_tag.HasTag(args.Used, ScoopTag))
        {
            if (ent.Comp.CurrentFish.Count <= 0)
            {
                _popup.PopupEntity(Loc.GetString("aquaponics-tank-message-scooped-empty"),
                    ent,
                    args.User,
                    PopupType.Medium);
            }

            foreach (var fish in ent.Comp.CurrentFish)
            {
                var speciesId = fish.SpeciesId;
                if (!_proto.TryIndex<FishSpeciesPrototype>(speciesId, out var fishSpecies))
                {
                    Log.Error($"Invalid or null fish species during scoop event: {speciesId}");
                    continue;
                }

                if (fish.IsDead)
                {
                    // Remove immediately since we're returning
                    ent.Comp.CurrentFish.Remove(fish);
                    _agingMultipliers.Remove(fish);

                    _popup.PopupEntity(Loc.GetString("aquaponics-tank-message-scooped-dead-fish",
                            ("species", fishSpecies.Name)),
                        ent,
                        args.User,
                        PopupType.Medium);

                    _popup.PopupEntity(Loc.GetString("aquaponics-tank-message-scooped-others"),
                        args.User,
                        Filter.PvsExcept(args.User),
                        false);

                    args.Handled = true;
                    return; // Now it's safe to return
                }

                if (fish.GrowthStage is (FishGrowthStage.Adult or FishGrowthStage.Elder))
                {
                    // Remove immediately since we're returning
                    ent.Comp.CurrentFish.Remove(fish);
                    _agingMultipliers.Remove(fish);

                    _popup.PopupEntity(Loc.GetString("aquaponics-tank-message-scooped",
                            ("species", fishSpecies.Name)),
                        args.User,
                        args.User,
                        PopupType.Medium);

                    _popup.PopupEntity(Loc.GetString("aquaponics-tank-message-scooped-others"),
                        args.User,
                        Filter.PvsExcept(args.User),
                        false);

                    var coords = Transform(ent).Coordinates;
                    var spawnedFish = Spawn(fishSpecies.HarvestedItemId, coords);
                    var ensuredFish = EnsureComp<GeneticFishComponent>(spawnedFish);

                    ensuredFish.SpeciesId = fish.SpeciesId;
                    ensuredFish.Genes = fish.Genes.ToList();
                    ensuredFish.GeneticInstability = fish.GeneticInstability;

                    args.Handled = true;
                    return;
                }
            }
        }

        if (!_solutionContainer.TryGetSolution(ent.Owner, "insertion", out var insSolComp, out var insSol))
            return;

        if (TryComp<SolutionContainerManagerComponent>(args.Used, out _))
        {
            if (_solutionContainer.TryGetSolution(args.Used, "food", out _, out var foodSol))
            {
                var splitSol = foodSol.SplitSolution(foodSol.Volume);
                _solutionContainer.AddSolution(insSolComp.Value, splitSol);

                if (TryComp<ProduceComponent>(args.Used, out var produceComp))
                    QueueDel(args.Used);

                args.Handled = true;
                return;
            }
        }
    }

    /// <summary>
    ///     A helper method to create a new fish from a species prototype.
    /// </summary>
    /// <param name="fishSpecies">The fish species to take data from</param>
    /// <returns>Returns a new fish entity</returns>
    private static FishData CreateFishFromSpecies(FishSpeciesPrototype fishSpecies)
    {
        return new FishData
        {
            SpeciesId =  fishSpecies.ID,
            Health = 100,
            Age = 0,
            Genes = [],
            GrowthStage = FishGrowthStage.Egg,
            GeneticInstability = 0,
        };
    }

    private float _updateTimer = 0f;
    private const float UpdateInterval = 1.0f;
    /// <summary>
    ///     The main update loop for the aquaponics tanks.
    /// </summary>
    /// <param name="frameTime" />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateTimer += frameTime;

        if (_updateTimer < frameTime)
            return;

        _updateTimer -= UpdateInterval;

        // We start by getting all the active aquaponics tanks in the server.
        // We then iterate through each tank and the contained fish.
        var query = EntityQueryEnumerator<AquaponicsTankComponent>();
        while (query.MoveNext(out var tankUid, out var tankComp))
        {
            if (!TryComp<SolutionContainerManagerComponent>(tankUid, out _))
                continue;

            // Now we check for the solution components.
            if (!_solutionContainer.TryGetSolution(tankUid, "waste", out var wasteSolComp, out _))
                continue;

            if (!_solutionContainer.TryGetSolution(tankUid, "nutrients", out _, out var foodSol))
                continue;

            foreach (var fish in tankComp.CurrentFish)
            {
                // We update the fish's age and growth stage based on its aging multiplier, set when an egg is inserted.
                // Once an egg reaches 240 "age time", we set the next growth stage until it's an elder.
                // Then reset the time. Once the fish is an elder, we damage the fish instead.
                if (!_agingMultipliers.TryGetValue(fish, out var ageMult))
                {
                    ageMult = GetAgingTime(fish);
                    _agingMultipliers[fish] = ageMult;
                }

                fish.Age += UpdateInterval * ageMult;

                if (fish is { Age: >= 180, IsDead: false })
                {
                    switch (fish.GrowthStage)
                    {
                        case FishGrowthStage.Egg:
                            fish.GrowthStage = FishGrowthStage.Fry;
                            break;
                        case FishGrowthStage.Fry:
                            fish.GrowthStage = FishGrowthStage.Juvenile;
                            break;
                        case FishGrowthStage.Juvenile:
                            fish.GrowthStage = FishGrowthStage.Adult;
                            break;
                        case FishGrowthStage.Adult:
                            fish.GrowthStage = FishGrowthStage.Elder;
                            break;
                        case FishGrowthStage.Elder:
                            fish.Health -= 10;
                            break;
                    }

                    fish.Age = 0;
                }

                // Now we run additional methods. These include whether to damage or heal the fish,
                // adding waste to the tank, consuming food, increasing disease, and removing fish.
                if (!fish.IsDead)
                {
                    CheckToDamageOrHealFish(fish, tankUid, tankComp);

                    if (_random.Next(3) == 0)
                    {
                        var newWaste = new Solution();
                        var wasteProd = 0.2f * GetWasteProduction(fish);
                        if (wasteProd < 0.05)
                            wasteProd = 0.05f;

                        newWaste.AddReagent(fish.ProducingReagent, FixedPoint2.New(wasteProd));
                        _solutionContainer.TryAddSolution(wasteSolComp.Value, newWaste);
                    }

                    if (_random.Next(2) == 0)
                    {
                        var foodConsumption = 0.33f * GetFoodConsumption(fish);
                        if (foodConsumption < 0.05)
                            foodConsumption = 0.05f;

                        foodSol.RemoveSolution(foodConsumption);
                    }

                    if (_random.Next(4) == 0)
                    {
                        var disease = 0.05f * GetDiseaseMultiplier(fish);
                        if (disease < 0.05)
                            disease = 0.05f;

                        tankComp.DiseaseLevel += disease;
                    }
                }
            }

            UpdateTankVisuals()
            TransferInsertedSolutions(tankUid, tankComp);
            TransferWaste(tankUid);
        }
    }

    /// <summary>
    ///     A helper method to transfer nutrients from the insertion tank to the nutrient tank.
    /// </summary>
    /// <param name="tankUid"></param>
    private void TransferInsertedSolutions(EntityUid tankUid, AquaponicsTankComponent tankComp)
    {
        if (!_solutionContainer.TryGetSolution(tankUid, "insertion", out _, out var insSol))
            return;

        if (!_solutionContainer.TryGetSolution(tankUid, "nutrients", out _, out var foodSol))
            return;

        if (!_solutionContainer.TryGetSolution(tankUid, "waste", out _, out var wasteSol))
            return;

        foreach (var (reagent, volume) in insSol.Contents.ToList())
        {
            var isWaste = reagent.Prototype is "Ammonia";

            var foodAmountToAdd = FixedPoint2.Min(volume, foodSol.AvailableVolume);
            if (reagent.Prototype is "Nutriment")
            {
                foodSol.AddReagent(reagent.Prototype, foodAmountToAdd);
            }

            if (reagent.Prototype is "EZNutrient")
            {
                foodSol.AddReagent(reagent.Prototype, foodAmountToAdd * 2);
            }

            if (reagent.Prototype is "Dylovene")
            {
                tankComp.DiseaseLevel = Math.Clamp(tankComp.DiseaseLevel - 2.0f, 0, 100);
            }

            var wasteAmountToAdd = FixedPoint2.Min(volume, wasteSol.AvailableVolume);
            if (isWaste)
            {
                wasteSol.AddReagent(reagent.Prototype, wasteAmountToAdd);
            }

            insSol.RemoveReagent(reagent.Prototype, volume);
        }
    }

    /// <summary>
    ///     A helper method that checks whether to damage or heal fish.
    /// </summary>
    /// <param name="fish">The fish data</param>
    /// <param name="tankUid">Tank Entity ID</param>
    /// <param name="tankComp">Tank Component</param>
    private void CheckToDamageOrHealFish(FishData fish, EntityUid tankUid, AquaponicsTankComponent tankComp)
    {
        // If the fish's health is less than or equal to 0, we set it to a dead state and return.
        if (fish.Health <= 0)
        {
            fish.IsDead = true;
            return;
        }

        // Now we check for the waste solution in the tank itself.
        // This is just so we can damage the fish based on its health.
        if (!_solutionContainer.TryGetSolution(tankUid, "waste", out _, out var wasteSol))
            return;

        // Then we set variables for damaging and healing, as well as check for conditions.
        // If the tank's waste volume is greater than 65, we damage the fish.
        // If the tank's disease level is greater than 50, we damage the fish.
        // Otherwise, if it CAN HEAL and health is less than 100, we heal the fish.
        var canHeal = true;
        var damageAmount = 0;
        if (wasteSol.Volume >= 75)
        {
            damageAmount = -10;
            canHeal = false;
        }

        if (tankComp.DiseaseLevel >= 50)
        {
            damageAmount = -10;
            canHeal = false;
        }

        if (fish.GeneticInstability >= 60)
            canHeal = false;

        if (canHeal && fish.Health < 100)
            damageAmount = 10;

        // Now we clamp the health to 0-100 and set the damage or heal amount.
        fish.Health = Math.Clamp(fish.Health + damageAmount, 0, 100);
    }

    /// <summary>
    ///     A helper method to transfer waste from the tank to an internal beaker.
    /// </summary>
    /// <param name="tankUid">Tank Entity UID</param>
    private void TransferWaste(EntityUid tankUid)
    {
        // First, we check for the waste solution in the tank itself.
        // As well as the internal beaker slot.
        if (!_solutionContainer.TryGetSolution(tankUid, "waste", out _, out var wasteSol))
            return;

        if (!_itemSlots.TryGetSlot(tankUid, "beaker", out var tankSlot))
            return;

        // If the beaker slot has an item and the item is NOT null, we start the code to transfer.
        if (tankSlot is { HasItem: true, Item: not null })
        {
            // We check for a beaker solution first, just to make sure we CAN transfer.
            if (!_solutionContainer.TryGetSolution(tankSlot.Item.Value, "beaker", out var beakerSolComp, out _))
                return;

            // Now we set a fixed point value for the volume to transfer.
            var vol = FixedPoint2.New(10);
            if (wasteSol.Volume < 10)
                return;

            // Now finally, we transfer the solution by splitting it from the tank
            // and adding it to the beaker solution.
            var transSol = wasteSol.SplitSolution(vol);
            _solutionContainer.TryAddSolution(beakerSolComp.Value, transSol);
        }
    }

    /// <summary>
    ///     A helper method to calculate the aging multiplier for fish.
    /// </summary>
    /// <param name="fish">The fish data</param>
    /// <returns></returns>
    private static float GetAgingTime(FishData fish)
    {
        // We start by setting the base mult to 1.0.
        // Now we iterate through the fish's genes and check for specific ones.
        // In this case, we check for Fast or Slow growth genes.
        // Fast decreases the mult (makes it faster), while slow increases it (makes it slower).
        var multiplier = 1.0f;
        foreach (var gene in fish.Genes)
        {
            multiplier = gene.ID switch
            {
                "TP14GeneFastGrowth" => 0.7f * gene.Strength,
                "TP14GeneSlowGrowth" => 1.3f * gene.Strength,
                _ => multiplier
            };
        }

        // Finally a check to make sure we never go below 0.0, and then we return the mult.
        if (multiplier <= 0.0f)
            multiplier = 0.1f;

        return multiplier;
    }

    /// <summary>
    ///     A helper method to get a fish's waste production rate.
    /// </summary>
    /// <param name="fish">Fish data</param>
    /// <returns>Returns a waste production multiplier</returns>
    private static float GetWasteProduction(FishData fish)
    {
        var mult = 1.0f;
        foreach (var gene in fish.Genes)
        {
            mult = gene.ID switch
            {
                "TP14GeneFastMetabolism" => 1.2f * gene.Strength,
                "TP14GeneSlowMetabolism" => 0.8f * gene.Strength,
                "TP14GeneWasteful" => 1.3f * gene.Strength,
                "TP14GeneConservative" => 0.7f * gene.Strength,
                _ => mult
            };
        }

        if (mult <= 0.0f)
        {
            mult = 0.1f;
        }

        return mult;
    }

    /// <summary>
    ///     A helper method to a fish's food consumption rate.
    /// </summary>
    /// <param name="fish">The fish data</param>
    /// <returns>Returns a food consumption rate multiplier.</returns>
    private static float GetFoodConsumption(FishData fish)
    {
        var multiplier = 1.0f;
        foreach (var gene in fish.Genes)
        {
            multiplier = gene.ID switch
            {
                "TP14GeneSlowMetabolism" => 0.7f * gene.Strength,
                "TP14GeneFastMetabolism" => 1.3f * gene.Strength,
                _ => multiplier
            };
        }

        // Finally a check to make sure we never go below 0.0, and then we return the mult.
        if (multiplier <= 0.0f)
            multiplier = 0.1f;

        return multiplier;
    }

    /// <summary>
    ///     A helper method to get a fish's disease multiplier.
    /// </summary>
    /// <param name="fish">The fish data</param>
    /// <returns>Returns a disease rate multiplier.</returns>
    private static float GetDiseaseMultiplier(FishData fish)
    {
        var multiplier = 1.0f;
        foreach (var gene in fish.Genes)
        {
            multiplier = gene.ID switch
            {
                "TP14GeneDiseaseProne" => 1.4f * gene.Strength,
                "TP14GeneDiseaseResistant" => 0.6f * gene.Strength,
                _ => multiplier
            };
        }

        // Finally a check to make sure we never go below 0.0, and then we return the mult.
        if (multiplier <= 0.0f)
            multiplier = 0.1f;

        return multiplier;
    }
}
