using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared._TP.Research.Planktology;
using Content.Shared._TP.Research.Planktology.Components;
using Content.Shared.Administration;
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
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly PlanktonDiet[] Diets = Enum.GetValues<PlanktonDiet>();

    private static readonly PlanktonCharacteristics[] Characteristics = Enum.GetValues<PlanktonCharacteristics>()
        .Where(c => c != PlanktonCharacteristics.None)
        .ToArray();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanktonComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlanktonComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<PlanktonComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PlanktonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var changed = false;

            if (now >= comp.NextInteraction)
            {
                comp.NextInteraction = now + comp.InteractionInterval;
                Interact(comp);
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

    private void OnMapInit(Entity<PlanktonComponent> ent, ref MapInitEvent args)
    {
        var now = _timing.CurTime;
        ent.Comp.NextInteraction = now + ent.Comp.InteractionInterval;
        ent.Comp.NextMetabolism = now + ent.Comp.MetabolismInterval;

        if (ent.Comp.InitialSpecies > 0)
            GenerateSpecies(ent, ent.Comp.InitialSpecies);
    }

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
    ///     Species feed and attack each other.
    /// </summary>
    private void Interact(PlanktonComponent comp)
    {
        foreach (var species in comp.Species)
        {
            if (!species.Alive)
                continue;

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
                    species.Hunger += comp.PhotosynthesisRate;
                    break;
            }

            species.Hunger = MathF.Min(species.Hunger, comp.MaxHunger);
        }
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

    private void OnIrradiated(Entity<PlanktonComponent> ent, ref OnIrradiatedEvent args)
    {
        var rads = args.RadsPerSecond * args.FrameTime;
        if (rads <= 0f)
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
}
