using System.Linq;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Power.Components;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Systems;
using Content.Shared._TP.Plankton;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mind.Components;
using Content.Shared.Radiation.Components;

namespace Content.Server._TP.Plankton.PlanktonSystems;

public sealed partial class PlanktonSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;

    private void UpdateDiets(PlanktonComponent planktonComp, EntityUid planktonUid)
    {
        foreach (var planktonSpecies in planktonComp.SpeciesInstances.Where(planktonSpecies => planktonSpecies.IsAlive))
        {
            if (!planktonSpecies.IsAlive)
                continue;

            if (planktonSpecies.CurrentHunger >= 45)
                continue;

            switch (planktonSpecies.Diet)
            {
                case PlanktonComponent.PlanktonDiet.Carnivore:
                    PerformCarnivoreDiet(planktonSpecies, planktonComp);
                    break;
                case PlanktonComponent.PlanktonDiet.Chemophage:
                    PerformChemophageDiet(planktonUid, planktonSpecies);
                    break;
                case PlanktonComponent.PlanktonDiet.Decomposer:
                    PerformDecomposerDiet(planktonSpecies, planktonComp);
                    break;
                case PlanktonComponent.PlanktonDiet.Electrotroph:
                    PerformElectrotrophDiet(planktonUid, planktonSpecies);
                    break;
                case PlanktonComponent.PlanktonDiet.Phototroph:
                    PerformPhototrophDiet(planktonUid, planktonSpecies);
                    break;
                case PlanktonComponent.PlanktonDiet.Radiophage:
                    PerformRadiophageDiet(planktonUid, planktonSpecies);
                    break;
                case PlanktonComponent.PlanktonDiet.Hemophage:
                    PerformHemophageDiet(planktonUid, planktonSpecies);
                    break;
                case PlanktonComponent.PlanktonDiet.Symbiotroph:
                    PerformSymbiotrophDiet(planktonSpecies, planktonComp);
                    break;
            }
        }
    }

    private void PerformSymbiotrophDiet(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent planktonComp)
    {
        if (planktonComp.SpeciesInstances.Count == 1)
            return;

        foreach (var allSpecies in planktonComp.SpeciesInstances)
        {
            if (allSpecies == planktonSpecies)
                continue;

            if (!allSpecies.IsAlive)
                continue;

            planktonSpecies.CurrentHunger += 5F;
        }
    }

    private void PerformHemophageDiet(EntityUid planktonUid, PlanktonComponent.PlanktonSpeciesInstance planktonSpecies)
    {
        if (!_solutionContainer.TryGetSolution(planktonUid, "input", out var inputEnt, out var inputSol))
            return;

        foreach (var blood in PlanktonComponent.BloodReagents)
        {
            var reagentAmount = inputSol.GetTotalPrototypeQuantity(blood);
            if (reagentAmount > 0)
            {
                var consumed = Math.Min(1F, reagentAmount.Value);
                inputSol.RemoveReagent(blood, consumed);
                planktonSpecies.CurrentHunger += consumed * 5f;
                planktonSpecies.CurrentHunger = Math.Min(50f, planktonSpecies.CurrentHunger);
            }
        }
    }

    private void PerformRadiophageDiet(EntityUid planktonUid, PlanktonComponent.PlanktonSpeciesInstance planktonSpecies)
    {
        if (!TryComp<RadiationReceiverComponent>(planktonUid, out var radiationReceiverComp))
        {
            _radiation.SetCanReceive(planktonUid, true);
            return;
        }

        if (radiationReceiverComp.CurrentRadiation > 0)
            planktonSpecies.CurrentHunger += 5F;
    }

    private void PerformPhototrophDiet(EntityUid entityUid, PlanktonComponent.PlanktonSpeciesInstance planktonSpecies)
    {
        if (!TryComp<PlanktonTankComponent>(entityUid, out var tankComp))
            return;

        if (!tankComp.LightEnabled)
            return;

        planktonSpecies.CurrentHunger += 5F;
    }

    private void PerformElectrotrophDiet(EntityUid planktonUid, PlanktonComponent.PlanktonSpeciesInstance planktonSpecies)
    {
        if (!TryComp<PlanktonTankComponent>(planktonUid, out var planktonComponent))
            return;

        if (!TryComp<ApcPowerReceiverComponent>(planktonUid, out var receiverComponent))
            return;

        if (planktonComponent.IsPowered)
        {
            if (receiverComponent.PowerReceived <= 0)
                return;

            planktonSpecies.CurrentHunger += 5F;
        }
    }

    private void PerformDecomposerDiet(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent planktonComp)
    {
        if (planktonComp.SpeciesInstances.Count == 1)
            return;

        foreach (var allSpecies in planktonComp.SpeciesInstances)
        {
            if (allSpecies == planktonSpecies)
                continue;

            if (allSpecies.IsAlive || planktonComp.DeadPlankton <= 0)
                continue;

            planktonComp.DeadPlankton -= 1F;
            planktonSpecies.CurrentHunger += 5F;

            if (planktonComp.DeadPlankton < 0)
                planktonComp.DeadPlankton = 0;
        }
    }

    private void PerformChemophageDiet(EntityUid planktonUid, PlanktonComponent.PlanktonSpeciesInstance planktonSpecies)
    {
        if (planktonSpecies.PreferredReagent == null)
            return;

        if (!_solutionContainer.TryGetSolution(planktonUid, "input", out var inputEnt, out var inputSol))
            return;

        var reagentAmount = inputSol.GetTotalPrototypeQuantity(planktonSpecies.PreferredReagent.Value.ToString());
        if (reagentAmount > 0)
        {
            var consumed = Math.Min(1F, reagentAmount.Value);
            inputSol.RemoveReagent(planktonSpecies.PreferredReagent.Value, consumed);
            planktonSpecies.CurrentHunger += consumed * 5f;
            planktonSpecies.CurrentHunger = Math.Min(50f, planktonSpecies.CurrentHunger);
        }
    }

    private void PerformCarnivoreDiet(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent planktonComp)
    {
        if (planktonComp.SpeciesInstances.Count == 1)
            return;

        foreach (var allSpecies in planktonComp.SpeciesInstances)
        {
            if (allSpecies == planktonSpecies)
                continue;

            if (!allSpecies.IsAlive)
                continue;

            allSpecies.CurrentSize -= 1F;
            planktonComp.DeadPlankton += 1F;
            planktonSpecies.CurrentHunger += 5F;

            if (allSpecies.CurrentSize <= 0)
            {
                allSpecies.CurrentSize = 0;
                allSpecies.IsAlive = false;
            }
        }
    }
}
