using System.Linq;
using Content.Shared._TP.Aquaponics.Components;
using Content.Shared._TP.Aquaponics.Prototypes;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.Aquaponics.Systems;

public sealed class FishEggSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishEggComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    ///     Event handler for ExaminedEvent.
    /// </summary>
    /// <param name="ent">FishEgg Entity</param>
    /// <param name="args">ExaminedEvent Arguments</param>
    private void OnExamined(Entity<FishEggComponent> ent, ref ExaminedEvent args)
    {
        var speciesID = ent.Comp.SpeciesId;
        if (!_proto.TryIndex<FishSpeciesPrototype>(speciesID, out var fishSpecies))
        {
            Log.Error($"Invalid or null fish species during examine event: {speciesID}");
            return;
        }

        var totalGenes = new List<string>();
        totalGenes.AddRange(fishSpecies.LockedFishGenes);
        foreach (var baseGenes in fishSpecies.BaseFishGenes.Where(baseGenes => !totalGenes.Contains(baseGenes)))
        {
            totalGenes.Add(baseGenes);
        }

        var genesStr = string.Join(", ", totalGenes);
        args.PushMarkup(Loc.GetString("fish-egg-examine-name", ("name", fishSpecies.Name)), 1);
        args.PushMarkup(Loc.GetString("fish-egg-examine-species", ("species", fishSpecies.Species)), 2);
        args.PushMarkup(Loc.GetString("fish-egg-examine-tier", ("tier", fishSpecies.FishTier)), 3);
        args.PushMarkup(Loc.GetString("fish-egg-examine-genes", ("genes", genesStr)), 4);
    }
}
