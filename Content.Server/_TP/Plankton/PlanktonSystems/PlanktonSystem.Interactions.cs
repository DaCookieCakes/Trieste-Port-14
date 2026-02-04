using System.Linq;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._TP.Plankton;
using Robust.Shared.Random;

namespace Content.Server._TP.Plankton.PlanktonSystems;

public sealed partial class PlanktonSystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    private void UpdateInteractions(PlanktonComponent planktonComp, EntityUid planktonUid)
    {
        foreach (var planktonSpecies in planktonComp.SpeciesInstances.Where(planktonSpecies => planktonSpecies.IsAlive))
        {
            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.Aggressive))
                PerformAggression(planktonSpecies, planktonComp);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.Mimicry))
                PerformMimicry(planktonSpecies, planktonComp);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.PheromoneGlands))
                PerformPheromone(planktonSpecies, planktonComp);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.Pyrotechnic))
                PerformPyrotechnics(planktonUid, planktonSpecies, planktonComp);
        }
    }

    /// <summary>
    ///     Pheromone performing
    /// </summary>
    /// <param name="planktonSpecies">Plankton Species</param>
    /// <param name="planktonComp">Plankton Component</param>
    private void PerformPheromone(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent planktonComp)
    {
        // If the species is alone or dead, return early.
        if (planktonComp.SpeciesInstances.Count == 1)
            return;

        if (!planktonSpecies.IsAlive)
            return;

        // Now we iterate through the components species.
        // if the species is the same as our main species,
        // or if the neighbors are dead, continue past them.
        foreach (var allSpecies in planktonComp.SpeciesInstances)
        {
            if (allSpecies == planktonSpecies)
                continue;

            if (!allSpecies.IsAlive)
                continue;

            // We flip a coin. If 'heads' (0.5F)...
            //
            // We flip ANOTHER coin. If that one is 'heads' - We Increase the other plankton food.
            // If 'tails' - Decrease food.
            //
            // If the first coin is 'tails'...
            //
            // We flip YET ANOTHER coin. If that one is 'heads' - We increase the colony size by 2.5
            // If 'tails' - decrease size
            if (_random.Prob(0.5F))
            {
                if (_random.Prob(0.5F))
                {
                    allSpecies.CurrentHunger += 2F;
                    allSpecies.CurrentHunger = Math.Min(50f, allSpecies.CurrentHunger);
                }
                else
                {
                    allSpecies.CurrentHunger -= 2F;
                }
            }
            else
            {
                if (_random.Prob(0.5F))
                {
                    allSpecies.CurrentSize += 2.5F;
                }
                else
                {
                    allSpecies.CurrentSize = Math.Min(0F, allSpecies.CurrentHunger - 2.5F);
                }
            }
        }
    }

    /// <summary>
    ///     Spawns a tiny, non-destructive explosion
    /// </summary>
    /// <param name="planktonUid">Plankton Uid</param>
    /// <param name="planktonSpecies">Plankton Species</param>
    /// <param name="planktonComp">Plankton Component</param>
    private void PerformPyrotechnics(EntityUid planktonUid, PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent planktonComp)
    {
        if (planktonComp.SpeciesInstances.Count == 1)
            return;

        if (!planktonSpecies.IsAlive)
            return;

        _explosion.QueueExplosion(
            planktonUid,
            "Default",
            1f,
            1,
            0.1f,
            0f,
            0,
            false
        );
    }

    private void PerformMimicry(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent planktonComp)
    {
        if (planktonComp.SpeciesInstances.Count == 1)
            return;

        if (!planktonSpecies.IsAlive)
            return;

        foreach (var allSpecies in planktonComp.SpeciesInstances)
        {
            if (allSpecies != planktonSpecies)
                continue;

            if (_random.Prob(0.5F))
            {
                planktonSpecies.SpeciesName = allSpecies.SpeciesName;
            }
            else if (_random.Prob(0.25F))
            {
                var firstName = PlanktonComponent.PlanktonFirstNames[
                    _random.Next(PlanktonComponent.PlanktonFirstNames.Length)];

                var secondName = PlanktonComponent.PlanktonSecondNames[
                    _random.Next(PlanktonComponent.PlanktonSecondNames.Length)];

                planktonSpecies.SpeciesName = new PlanktonComponent.PlanktonName(firstName, secondName);
            }
        }
    }

    private void PerformAggression(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent planktonComp)
    {
        foreach (var allSpecies in planktonComp.SpeciesInstances)
        {
            if (allSpecies == planktonSpecies)
                continue;

            if (!allSpecies.IsAlive)
                continue;

            if (HasCharacteristic(allSpecies, PlanktonComponent.PlanktonCharacteristics.PheromoneGlands))
                continue;

            allSpecies.CurrentSize -= 2.5F;
            planktonComp.DeadPlankton += 2.5F;

            if (allSpecies.CurrentSize <= 0)
            {
                allSpecies.CurrentSize = 0;
                allSpecies.IsAlive = false;
            }
        }
    }
}
