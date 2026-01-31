using System.Linq;
using Content.Shared._TP.Plankton;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Radiation.Events;
using Robust.Server.GameObjects;

namespace Content.Server._TP.Plankton;

public sealed class PlanktonSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    private const float UpdateInterval = 1f; // Interval in seconds
    private const float HungerInterval = 5f;

    private float _updateTimer;
    private float _hungerTimer;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlanktonComponent, ComponentInit>(OnPlanktonCompInit);
        SubscribeLocalEvent<PlanktonComponent, OnIrradiatedEvent>(OnRadiation);
        SubscribeLocalEvent<PlanktonComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, PlanktonComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        foreach (var plankton in component.SpeciesInstances)
        {
            if (!plankton.IsAlive)
            {
                args.PushMarkup($"The {plankton.SpeciesName} colony is dead!");
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        _hungerTimer += frameTime;

        if (_updateTimer >= UpdateInterval)
        {
            foreach (var entity in EntityManager.EntityQuery<PlanktonComponent>())
            {
                var uid = entity.Owner; // entity.Owner is the EntityUid
                PlanktonInteraction(uid);
            }

            _updateTimer = 0f;
        }

        if (_hungerTimer >= HungerInterval)
        {
            foreach (var entity in EntityManager.EntityQuery<PlanktonComponent>())
            {
                PlanktonHunger(entity);
                PlanktonGrowth(entity);
            }

            _hungerTimer = 0f;
        }
    }

    private void OnPlanktonCompInit(EntityUid uid, PlanktonComponent component, ComponentInit args)
    {
        Log.Info($"Plankton component initialized");
    }

    private void PlanktonInteraction(EntityUid uid)
    {
        if (!HasComp<PlanktonComponent>(uid))
        {
            Log.Error($"No PlanktonComponent found for entity {uid}");
            return;
        }

        var component = _entityManager.GetComponent<PlanktonComponent>(uid);
        CheckPlanktonCharacteristics(component, uid);
        CheckPlanktonDiet(component, uid);
    }

    private void CheckPlanktonCharacteristics(PlanktonComponent component, EntityUid uid)
    {
        foreach (var planktonInstance in component.SpeciesInstances)
        {
            if (planktonInstance.IsAlive)
            {
                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
                {
                    PerformAggressionCheck(component);
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Bioluminescent) != 0)
                {
                    if (!HasComp<PointLightComponent>(uid))
                        EnsureComp<PointLightComponent>(uid);

                    _pointLight.SetEnabled(uid, true);
                    _pointLight.SetEnergy(uid, 8F);
                    _pointLight.SetRadius(uid, 1F);
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Charged) != 0)
                {
                    if (!HasComp<ElectrifiedComponent>(uid))
                        EnsureComp<ElectrifiedComponent>(uid).RequirePower = false;
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Mimicry) != 0)
                {

                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.ChemicalProduction) != 0)
                {

                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.MagneticField) != 0)
                {

                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Hallucinogenic) != 0)
                {
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.PheromoneGlands) != 0)
                {
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.PolypColony) != 0)
                {
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.AerosolSpores) != 0)
                {
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0)
                {
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Sentience) != 0)
                {
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Pyrophilic) != 0)
                {
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Cryophilic) != 0)
                {
                }
            }
        }
    }

    /// <summary>
    ///     Radiation damage (or eating) process for Plankton species.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="component">Plankton Component</param>
    /// <param name="args">OnIrradiatedEvent arguments</param>
    private void OnRadiation(EntityUid uid, PlanktonComponent component, OnIrradiatedEvent args)
    {
        foreach (var planktonInstance in component.SpeciesInstances)
        {
            if (!planktonInstance.IsAlive)
                continue;

            // If it's a Radiophage Plankton species, we fill hunger. Yum yum!
            // Otherwise, harm the Plankton with Uranium. Bombs!
            if (planktonInstance.Diet == PlanktonComponent.PlanktonDiet.Radiophage)
            {
                planktonInstance.CurrentHunger += 0.5f;
                planktonInstance.CurrentHunger = Math.Min(50f, planktonInstance.CurrentHunger);
                Log.Info($"{planktonInstance.SpeciesName} has eaten radiaiton to {planktonInstance.CurrentHunger}");
            }
            else
            {
                planktonInstance.CurrentSize -= (float)0.5;
                component.DeadPlankton += (float)0.5;

                Log.Info($"{planktonInstance.SpeciesName} is dying due to radiation exposure! Current size is {planktonInstance.CurrentSize}");

                if (planktonInstance.CurrentSize <= 0)
                {
                    planktonInstance.IsAlive = false;
                    Log.Info($"{planktonInstance.SpeciesName} has been killed by excess radiation exposure");
                }
            }
        }
    }

    private void CheckPlanktonDiet(PlanktonComponent component, EntityUid uid)
    {
        foreach (var planktonInstance in component.SpeciesInstances)
        {
            if (!planktonInstance.IsAlive)
                continue;

            switch (planktonInstance.Diet)
            {
                case PlanktonComponent.PlanktonDiet.Decomposer:
                {
                    if (component.DeadPlankton > 0)
                        DecomposeCheck(planktonInstance, component);

                    break;
                }
                case PlanktonComponent.PlanktonDiet.Carnivore:
                {
                    PerformCarnivoreCheck(component);
                    break;
                }
                case PlanktonComponent.PlanktonDiet.Photosynthetic:
                {
                    const float photosyntheticFood = 0.001f;
                    planktonInstance.CurrentHunger += photosyntheticFood;
                    Log.Info($"{planktonInstance.SpeciesName} photosynthesizing");
                    break;
                }
            }
        }
    }

    private void DecomposeCheck(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component)
    {
        const float sizeGrowth = 0.2f;
        component.DeadPlankton -= sizeGrowth;
        planktonInstance.CurrentHunger += sizeGrowth;

        if (component.DeadPlankton < 0)
        {
            component.DeadPlankton = 0;
            Log.Info($"All dead plankton have been eaten.");
        }

        planktonInstance.CurrentHunger += sizeGrowth;
        Log.Info($"Increased satiation of {planktonInstance.SpeciesName} to {planktonInstance.CurrentHunger} from decomposing food.");
        Log.Info($"There is {component.DeadPlankton} food left.");
    }

    private void PerformAggressionCheck(PlanktonComponent component)
    {

        var planktonToRemove = new List<PlanktonComponent.PlanktonSpeciesInstance>();

        foreach (var planktonEntity in EntityManager.EntityQuery<PlanktonComponent>())
        {
            var aggressivePlanktonInstances = component.SpeciesInstances
                .Where(inst => (inst.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
                .ToList();

            if (aggressivePlanktonInstances.Any())
            {
                foreach (var aggressivePlankton in aggressivePlanktonInstances)
                {
                    foreach (var otherPlankton in component.SpeciesInstances)
                    {

                        if (aggressivePlankton == otherPlankton)
                            continue;

                        if (!otherPlankton.IsAlive)
                            continue;

                        if (otherPlankton.IsAlive && aggressivePlankton.IsAlive)
                            ReducePlanktonSizeAggression(otherPlankton, component, aggressivePlankton);

                        if (otherPlankton.CurrentSize <= 0)
                            planktonToRemove.Add(otherPlankton);
                    }
                }
            }
        }


        foreach (var plankton in planktonToRemove)
        {
            plankton.IsAlive = false;
        }
    }


    private void PerformCarnivoreCheck(PlanktonComponent component)
    {
        var planktonToRemoveCarnivore = new List<PlanktonComponent.PlanktonSpeciesInstance>();

        foreach (var planktonEntity in EntityManager.EntityQuery<PlanktonComponent>())
        {

            var carnivorousPlanktonInstances = component.SpeciesInstances
                .Where(inst => (inst.Diet == PlanktonComponent.PlanktonDiet.Carnivore))
                .ToList();


            if (carnivorousPlanktonInstances.Any())
            {
                foreach (var carnivorousPlankton in carnivorousPlanktonInstances)
                {
                    int carnivoreCount = (int)carnivorousPlankton.CurrentSize;
                    float huntMultiplier = carnivoreCount * 0.005f;
                    foreach (var otherPlankton in component.SpeciesInstances)
                    {
                        if (carnivorousPlankton == otherPlankton)
                            continue;
                        if (otherPlankton.IsAlive == true && carnivorousPlankton.IsAlive == true)
                        {
                            float sizeReduction = 0.1f + huntMultiplier;
                            ReducePlanktonSizeCarnivorous(otherPlankton, component, carnivorousPlankton, sizeReduction);
                        }

                        if (!otherPlankton.IsAlive)
                        {
                            continue;
                        }

                        // Check if the plankton instance should be removed
                        if (otherPlankton.CurrentSize <= 0)
                        {
                            planktonToRemoveCarnivore.Add(otherPlankton);
                        }
                    }
                }
            }
        }



        // Remove the dead plankton species instances after the loop
        foreach (var plankton in planktonToRemoveCarnivore)
        {
            plankton.IsAlive = false;
        }
    }



    private void ReducePlanktonSizeCarnivorous(PlanktonComponent.PlanktonSpeciesInstance planktonInstance,
        PlanktonComponent component,
        PlanktonComponent.PlanktonSpeciesInstance carnivorousPlankton,
        float sizeReduction)
    {
        planktonInstance.CurrentSize -= sizeReduction;
        Log.Info(
            $"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize} via being predated on by {carnivorousPlankton.SpeciesName}");
        carnivorousPlankton.CurrentHunger += sizeReduction;
        Log.Info($"{carnivorousPlankton.SpeciesName} is now at {carnivorousPlankton.CurrentHunger} after hunting");

        if (planktonInstance.CurrentSize <= 0)
        {
            planktonInstance.CurrentSize = 0;
            planktonInstance.IsAlive = false;
            Log.Info($"{planktonInstance.SpeciesName} has been wiped out by {carnivorousPlankton.SpeciesName}.");
        }
    }

    private void ReducePlanktonSizeAggression(PlanktonComponent.PlanktonSpeciesInstance planktonInstance,
        PlanktonComponent component,
        PlanktonComponent.PlanktonSpeciesInstance aggressivePlankton)
    {
        float sizeReduction = 0.1f;
        planktonInstance.CurrentSize -= sizeReduction;
        component.DeadPlankton += sizeReduction;
        Log.Info(
            $"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize} via being aggressively attacked by {aggressivePlankton.SpeciesName} ");

        if (planktonInstance.CurrentSize <= 0)
        {
            planktonInstance.CurrentSize = 0;
            planktonInstance.IsAlive = false;
            Log.Info($"{planktonInstance.SpeciesName} has been wiped out by {aggressivePlankton.SpeciesName}.");
            // change IsAlive once the framework is finished
        }
    }

    private void PlanktonHunger(PlanktonComponent component)
    {
        foreach (var planktonInstance in component.SpeciesInstances)
        {
            if (!planktonInstance.IsAlive)
                continue;

            if (planktonInstance.CurrentHunger <= 0)
            {
                // If hunger is 0 or less, plankton dies
                if (planktonInstance.CurrentHunger <= 0f)
                {
                    planktonInstance.CurrentHunger = 0f;
                    component.DeadPlankton += planktonInstance.CurrentSize;
                    planktonInstance.IsAlive = false;
                    Log.Error($"{planktonInstance.SpeciesName} has starved to death.");
                }
            }
            else
            {
                // Reduce hunger if greater than 0
                const float hungerLoss = 0.5f;
                const float hungerIncrease = 0.01f;
                var hungerExponent = planktonInstance.CurrentSize * hungerIncrease + hungerLoss;

                planktonInstance.CurrentHunger -= hungerExponent;
                planktonInstance.CurrentHunger = Math.Max(0f, planktonInstance.CurrentHunger); // Ensure it doesn't go below 0

                Log.Error($"{planktonInstance.SpeciesName} has lost {hungerLoss} hunger. It is now at {planktonInstance.CurrentHunger}");
            }

            // Ensure hunger doesn't exceed 50 and log if full
            if (planktonInstance.CurrentHunger >= 51f)
            {
                planktonInstance.CurrentHunger = 50f;
                Log.Error($"{planktonInstance.SpeciesName} is full.");
            }
        }
    }

    private void PlanktonGrowth(PlanktonComponent component)
    {
        foreach (var planktonInstance in component.SpeciesInstances)
        {
            float growthRate;
            if (!planktonInstance.IsAlive)
                continue;

            if (planktonInstance is { CurrentSize: >= 200, CurrentHunger: >= 50 })
            {
                growthRate = 0.01f;
                Log.Info($"{planktonInstance.SpeciesName} is a class-III plankton");
            }
            else if (planktonInstance is { CurrentSize: <= 200, CurrentHunger: >= 45 })
            {
                growthRate = 0.02f;
                Log.Info($"{planktonInstance.SpeciesName} is a class-II plankton");
            }
            else if (planktonInstance is { CurrentSize: <= 100, CurrentHunger: >= 30 })
            {
                growthRate = 0.05f;
                Log.Info($"{planktonInstance.SpeciesName} is a class-I plankton");
            }
            else
            {
                continue;
            }

            var growthExponent = growthRate * planktonInstance.CurrentSize;
            planktonInstance.CurrentSize += growthExponent;
        }
    }
}
