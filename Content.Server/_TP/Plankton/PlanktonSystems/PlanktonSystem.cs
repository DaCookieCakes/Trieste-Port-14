using System.Linq;
using Content.Shared._TP.Plankton;
using Content.Shared.Examine;
using Robust.Shared.Random;

namespace Content.Server._TP.Plankton.PlanktonSystems;

/// <summary>
///     Main Plankton entity system class
/// </summary>
public sealed partial class PlanktonSystem : EntitySystem
{
    private const float UpdateInterval = 2.5f;
    private const float HungerInterval = 5f;

    private float _updateTimer;
    private float _hungerTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanktonComponent, ExaminedEvent>(OnExamine);
    }

    /// <summary>
    ///     Examination of the colony species
    /// </summary>
    /// <param name="planktonUid">PlanktonUid</param>
    /// <param name="planktonComp">Plankton Component</param>
    /// <param name="args">ExaminedEvent arguments</param>
    private void OnExamine(EntityUid planktonUid, PlanktonComponent planktonComp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        foreach (var plankton in planktonComp.SpeciesInstances)
        {
            if ((plankton.Characteristics & PlanktonComponent.PlanktonCharacteristics.Mimicry) != 0)
            {
                if (_random.Prob(0.75F) && plankton.IsAlive)
                    return;
            }

            args.PushMarkup(plankton.IsAlive
                ? Loc.GetString("plankton-examine-species", ("species", plankton.SpeciesName))
                :  Loc.GetString("plankton-examine-species-dead", ("species", plankton.SpeciesName)));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        _hungerTimer += frameTime;

        var shouldUpdateCharacteristics = _updateTimer >= UpdateInterval;
        var shouldUpdateHunger = _hungerTimer >= HungerInterval;

        // Main update loop, handles passive Characteristics.
        var query = EntityQueryEnumerator<PlanktonComponent>();
        while (query.MoveNext(out var planktonUid, out var planktonComp))
        {
            if (shouldUpdateCharacteristics)
            {
                UpdateCharacteristics(planktonComp, planktonUid);
                UpdateInteractions(planktonComp, planktonUid);
                _updateTimer = 0;
            }

            if (shouldUpdateHunger)
            {
                PlanktonHunger(planktonComp);
                UpdateDiets(planktonComp, planktonUid);
                _hungerTimer = 0;
            }
        }

        if (shouldUpdateCharacteristics)
            _updateTimer = 0;

        if (shouldUpdateHunger)
            _hungerTimer = 0;
    }

    private void PlanktonHunger(PlanktonComponent component)
    {
        foreach (var planktonInstance in component.SpeciesInstances.Where(planktonInstance => planktonInstance.IsAlive))
        {
            if (planktonInstance.CurrentHunger <= 0)
            {
                planktonInstance.CurrentSize -= 2.5F;
                component.DeadPlankton += 2.5F;

                if (planktonInstance.CurrentSize <= 0)
                {
                    planktonInstance.CurrentSize = 0;
                    planktonInstance.IsAlive = false;
                }

                Log.Error($"{planktonInstance.SpeciesName} is starving to death.");
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
            if (planktonInstance.CurrentHunger > 50f)
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
