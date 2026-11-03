using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Research.Planktology;
using Content.Shared._TP.Research.Planktology.Components;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Radiation.Events;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._TP.Research.Planktology.EntitySystems;

/// <summary>
///     Simulates the plankton living in a <see cref="PlanktonComponent"/> sample:
///         generation, feeding, fighting, hunger, and growth.
/// </summary>
public sealed partial class PlanktonSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    private static readonly PlanktonDiet[] Diets = Enum.GetValues<PlanktonDiet>();

    private static readonly PlanktonCharacteristics[] Characteristics = Enum.GetValues<PlanktonCharacteristics>()
        .Where(c => c != PlanktonCharacteristics.None)
        .ToArray();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PlanktonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Empty tanks still tick so their electrophage power draw resets.
            if (comp.Stasis || comp.Species.Count == 0 && !HasComp<PlanktonTankComponent>(uid))
                continue;

            var changed = false;

            if (now >= comp.NextInteraction)
            {
                comp.NextInteraction = now + comp.InteractionInterval;
                Interact(uid, comp);
                changed = true;
            }

            if (now >= comp.NextMetabolism)
            {
                comp.NextMetabolism = now + comp.MetabolismInterval;
                Grow(comp);
                Starve(comp);
                changed = true;
            }

            if (changed)
                Dirty(uid, comp);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PlanktonComponent> ent, ref MapInitEvent args)
    {
        var now = _timing.CurTime;
        ent.Comp.NextInteraction = now + ent.Comp.InteractionInterval;
        ent.Comp.NextMetabolism = now + ent.Comp.MetabolismInterval;

        if (ent.Comp.InitialSpecies > 0)
            GenerateSpecies(ent, ent.Comp.InitialSpecies);
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<PlanktonComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_admin.HasAdminFlag(args.User, AdminFlags.Debug))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("plankton-generate-verb"),
            Category = VerbCategory.Debug,
            Act = () => GenerateSpecies(ent, 3),
        });
    }

    #region Generation

    /// <summary>
    /// Adds <paramref name="count"/> randomly generated species to the sample.
    /// </summary>
    public void GenerateSpecies(Entity<PlanktonComponent> ent, int count)
    {
        var firstNames = _proto.Index(ent.Comp.FirstNames).Values;
        var secondNames = _proto.Index(ent.Comp.SecondNames).Values;

        for (var i = 0; i < count; i++)
        {
            ent.Comp.Species.Add(new PlanktonSpecies
            {
                Name = $"{_random.Pick(firstNames)} {_random.Pick(secondNames)}",
                Diet = _random.Pick(Diets),
                Characteristics = PickCharacteristics(_random.Next(2, 4)),
                Size = 1f,
                Hunger = ent.Comp.MaxHunger,
                Alive = true,
            });
        }

        Dirty(ent);
    }

    private PlanktonCharacteristics PickCharacteristics(int count)
    {
        var pool = new List<PlanktonCharacteristics>(Characteristics);
        var result = PlanktonCharacteristics.None;

        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var picked = _random.PickAndTake(pool);
            result |= picked;

            // It can't love both the cold and the heat.
            if (picked == PlanktonCharacteristics.Cryophilic)
                pool.Remove(PlanktonCharacteristics.Pyrophilic);
            else if (picked == PlanktonCharacteristics.Pyrophilic)
                pool.Remove(PlanktonCharacteristics.Cryophilic);
        }

        return result;
    }

    #endregion

    #region Simulation

    /// <summary>
    ///     Species feed, attack each other, and react to their tank's conditions.
    /// </summary>
    private void Interact(EntityUid uid, PlanktonComponent comp)
    {
        // Outside a tank there's no light, and the temperature doesn't matter.
        var tank = CompOrNull<PlanktonTankComponent>(uid);
        var powered = tank != null && _power.IsPowered(uid);
        var lit = powered && tank!.LightsOn;

        Entity<SolutionComponent>? water = null;
        if (tank != null && _solutions.TryGetSolution(uid, tank.SolutionName, out var solution))
            water = solution;

        var electrophageSize = 0f;

        foreach (var species in comp.Species)
        {
            if (!species.Alive)
                continue;

            if (tank != null && !IsComfortable(comp, species, tank.Temperature))
            {
                comp.DeadPlankton += Damage(species, comp.TemperatureDamage);
                if (!species.Alive)
                    continue;
            }

            if (species.Has(PlanktonCharacteristics.Aggressive))
            {
                foreach (var other in comp.Species)
                {
                    if (other == species || !other.Alive)
                        continue;

                    comp.DeadPlankton += Damage(other, comp.AggressionDamage);
                }
            }

            switch (species.Diet)
            {
                case PlanktonDiet.Carnivore:
                    var bite = comp.CarnivoreDamage + species.Size * comp.CarnivoreDamagePerSize;
                    foreach (var prey in comp.Species)
                    {
                        if (prey == species || !prey.Alive)
                            continue;

                        species.Hunger += Damage(prey, bite);
                    }
                    break;

                case PlanktonDiet.Decomposer:
                    var eaten = MathF.Min(comp.DecomposeRate, comp.DeadPlankton);
                    comp.DeadPlankton -= eaten;
                    species.Hunger += eaten;
                    break;

                case PlanktonDiet.Photosynthetic:
                    if (lit)
                        species.Hunger += comp.PhotosynthesisRate;
                    break;

                case PlanktonDiet.Electrophage:
                    if (!powered)
                        break;

                    species.Hunger += comp.ElectrophageRate;
                    electrophageSize += species.Size;
                    break;

                case PlanktonDiet.Chemophage:
                    if (water != null)
                        species.Hunger += EatReagent(water.Value, comp.ChemophageFoods, comp.ReagentEatRate) * comp.ReagentFoodValue;
                    break;

                case PlanktonDiet.Hemophage:
                    if (water != null)
                        species.Hunger += EatReagent(water.Value, comp.HemophageFoods, comp.ReagentEatRate) * comp.ReagentFoodValue;
                    break;
            }

            species.Hunger = MathF.Min(species.Hunger, comp.MaxHunger);
        }

        // Electrophages feed off the tank's power supply, so bigger colonies make it draw more.
        if (tank != null)
        {
            var extra = MathF.Min(electrophageSize * tank.ElectrophagePowerPerSize, tank.MaxElectrophagePower);
            _power.SetLoad(uid, tank.BasePowerLoad + extra);
        }
    }

    /// <summary>
    ///     Removes up to <paramref name="amount"/> of the first edible reagent found in the solution.
    /// </summary>
    /// <returns>Units actually eaten.</returns>
    private float EatReagent(Entity<SolutionComponent> solution, List<ProtoId<ReagentPrototype>> foods, FixedPoint2 amount)
    {
        foreach (var (reagent, quantity) in solution.Comp.Solution.Contents)
        {
            if (!foods.Contains(reagent.Prototype))
                continue;

            return _solutions.RemoveReagent(solution, reagent, FixedPoint2.Min(amount, quantity)).Float();
        }

        return 0f;
    }

    private static bool IsComfortable(PlanktonComponent comp, PlanktonSpecies species, float temperature)
    {
        if (species.Has(PlanktonCharacteristics.Cryophilic))
            return temperature <= comp.ColdThreshold;

        if (species.Has(PlanktonCharacteristics.Pyrophilic))
            return temperature >= comp.HotThreshold;

        return temperature > comp.ColdThreshold && temperature < comp.HotThreshold;
    }

    /// <summary>
    ///     Well-fed species grow exponentially; small cultures grow fastest.
    /// </summary>
    private static void Grow(PlanktonComponent comp)
    {
        foreach (var species in comp.Species)
        {
            if (!species.Alive)
                continue;

            float growthRate;
            if (species.Size < 100f && species.Hunger >= 30f)
                growthRate = 0.05f; // Class I
            else if (species.Size < 200f && species.Hunger >= 45f)
                growthRate = 0.02f; // Class II
            else if (species.Size >= 200f && species.Hunger >= comp.MaxHunger)
                growthRate = 0.01f; // Class III
            else
                continue;

            species.Size += species.Size * growthRate;
        }
    }

    /// <summary>
    ///     Species get hungrier, bigger cultures get hungry faster.
    ///     Starved species die.
    /// </summary>
    private static void Starve(PlanktonComponent comp)
    {
        foreach (var species in comp.Species)
        {
            if (!species.Alive)
                continue;

            species.Hunger -= comp.HungerLoss + species.Size * comp.HungerLossPerSize;
            if (species.Hunger > 0f)
                continue;

            species.Hunger = 0f;
            species.Alive = false;
            comp.DeadPlankton += species.Size;
        }
    }

    [SubscribeLocalEvent]
    private void OnIrradiated(Entity<PlanktonComponent> ent, ref OnIrradiatedEvent args)
    {
        Irradiate(ent, args.RadsPerSecond * args.FrameTime);
    }

    /// <summary>
    ///     Feeds radiophages and shrinks everything else. Frozen plankton are unaffected.
    /// </summary>
    public void Irradiate(Entity<PlanktonComponent> ent, float rads)
    {
        if (rads <= 0f || ent.Comp.Stasis)
            return;

        foreach (var species in ent.Comp.Species)
        {
            if (!species.Alive)
                continue;

            if (species.Diet == PlanktonDiet.Radiophage)
                species.Hunger = MathF.Min(species.Hunger + rads * ent.Comp.RadiationFood, ent.Comp.MaxHunger);
            else
                ent.Comp.DeadPlankton += Damage(species, rads * ent.Comp.RadiationDamage);
        }

        Dirty(ent);
    }

    /// <summary>
    ///     Shrinks a species, killing it at zero size.
    /// </summary>
    /// <returns>How much size was actually removed.</returns>
    private static float Damage(PlanktonSpecies species, float amount)
    {
        var dealt = MathF.Min(amount, species.Size);
        species.Size -= dealt;

        if (species.Size <= 0f)
        {
            species.Size = 0f;
            species.Alive = false;
        }

        return dealt;
    }

    #endregion

    #region Transfer

    /// <summary>
    ///     Moves every species and all dead plankton from one sample into another.
    /// </summary>
    /// <returns>How many species were moved.</returns>
    public int TransferAll(Entity<PlanktonComponent> from, Entity<PlanktonComponent> to)
    {
        var count = from.Comp.Species.Count;

        to.Comp.Species.AddRange(from.Comp.Species);
        to.Comp.DeadPlankton += from.Comp.DeadPlankton;
        from.Comp.Species.Clear();
        from.Comp.DeadPlankton = 0f;

        Dirty(from);
        Dirty(to);
        return count;
    }

    /// <summary>
    ///     Moves a single species from one sample into another.
    /// </summary>
    public void TransferSpecies(Entity<PlanktonComponent> from, Entity<PlanktonComponent> to, PlanktonSpecies species)
    {
        if (!from.Comp.Species.Remove(species))
            return;

        to.Comp.Species.Add(species);
        Dirty(from);
        Dirty(to);
    }

    #endregion
}
