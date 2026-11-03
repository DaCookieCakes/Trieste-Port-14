using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._TP.Research.Planktology;
using Content.Shared._TP.Research.Planktology.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._TP.Research.Planktology.EntitySystems;

/// <summary>
///     The Cryogenic filter. 
///     It holds deposited plankton in stasis and can be pulled back out,
///         one species at a time.
/// </summary>
public sealed partial class PlanktonSeparatorSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private PlanktonSystem _plankton = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<PlanktonSeparatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnContainerInserted(Entity<PlanktonSeparatorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<PlanktonSeparatorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnDeposit(Entity<PlanktonSeparatorComponent> ent, ref PlanktonSeparatorDepositMessage args)
    {
        if (!TryGetSamples(ent, out var frozen, out var container))
            return;

        _plankton.TransferAll(container.Value, frozen.Value);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnExtract(Entity<PlanktonSeparatorComponent> ent, ref PlanktonSeparatorExtractMessage args)
    {
        if (!TryGetSamples(ent, out var frozen, out var container))
            return;

        var species = frozen.Value.Comp.Species;
        if (args.Index < 0 || args.Index >= species.Count || species[args.Index].Name != args.Name)
            return;

        _plankton.TransferSpecies(frozen.Value, container.Value, species[args.Index]);
        UpdateUi(ent);
    }

    private bool TryGetSamples(Entity<PlanktonSeparatorComponent> ent,
        [NotNullWhen(true)] out Entity<PlanktonComponent>? frozen,
        [NotNullWhen(true)] out Entity<PlanktonComponent>? container)
    {
        frozen = null;
        container = null;

        if (!TryComp<PlanktonComponent>(ent, out var frozenComp))
            return false;

        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.ContainerSlotId) is not { } item
            || !TryComp<PlanktonComponent>(item, out var containerComp))
            return false;

        frozen = (ent, frozenComp);
        container = (item, containerComp);
        return true;
    }

    private void UpdateUi(Entity<PlanktonSeparatorComponent> ent)
    {
        if (!TryComp<PlanktonComponent>(ent, out var frozen))
            return;

        var entries = frozen.Species
            .Select((s, i) => new PlanktonSeparatorEntry(i, s.Name, s.Alive, s.Size))
            .ToList();

        var item = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.ContainerSlotId);
        var containerCount = item != null && TryComp<PlanktonComponent>(item, out var carried)
            ? carried.Species.Count
            : 0;

        _ui.SetUiState(ent.Owner,
            PlanktonSeparatorUiKey.Key,
            new PlanktonSeparatorBoundUserInterfaceState(entries, item != null, containerCount));
    }
}
