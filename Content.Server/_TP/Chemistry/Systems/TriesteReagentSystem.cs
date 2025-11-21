using Content.Shared._TP.Chemistry.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.Chemistry.Systems;

public sealed class TriesteReagentSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerManagerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<SolutionContainerManagerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!IsWearingChemGoggles(args.Examiner))
            return;

        foreach (var container in ent.Comp.Containers)
        {
            if (!_solutionContainer.TryGetSolution(ent.Owner,
                    container,
                    out _,
                    out var solName))
                continue;

            foreach (var reagentQuantity in solName.Contents)
            {
                var reagentId = reagentQuantity.Reagent.Prototype;

                // The reagent system is a mess, so we have to get the prototype ourselves.
                if (!_prototype.TryIndex<ReagentPrototype>(reagentId, out var reagentProto))
                    continue;

                TriesteReagentPrototype? props = null;
                foreach (var triesteProto in _prototype.EnumeratePrototypes<TriesteReagentPrototype>())
                {
                    if (triesteProto.Reagent == reagentId)
                    {
                        props = triesteProto;
                        break;
                    }
                }

                if (props == null)
                    continue;

                if (props.BoilingPoint > 0)
                {
                    args.PushMarkup(Loc.GetString("tp14-reagent-boiling-point",
                        ("reagent", reagentProto.LocalizedName),
                        ("temp", props.BoilingPoint.ToString("F3"))),
                        1);
                }

                var typeStr = string.Join(", ", props.Compounds);
                args.PushMarkup(Loc.GetString("tp14-reagent-compound-type",
                    ("reagent", reagentProto.LocalizedName),
                    ("types", typeStr)),
                    2);

                foreach (var compound in props.Compounds)
                {
                    args.PushMarkup(Loc.GetString("tp14-reagent-separation-methods",
                        ("method", compound.ToString()),
                        ("reagent", reagentProto.LocalizedName)),
                        3);
                }
            }
        }
    }

    private bool IsWearingChemGoggles(EntityUid argsExaminer)
    {
        return _inventory.TryGetSlotEntity(argsExaminer, "eyes", out var eyeWear)
               && HasComp<SolutionScannerComponent>(eyeWear);
    }
}
