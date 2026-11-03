using System.Diagnostics.CodeAnalysis;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Research.Planktology.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server._TP.Research.Planktology.EntitySystems;

/// <summary>
///     Draws sea water from a pipe and filters plankton colonies out of it into an inserted container.
///     Turned on and off with its power switch, and turns itself off after each harvest.
/// </summary>
public sealed partial class PlanktonHarvesterSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private PlanktonSystem _plankton = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    [SubscribeLocalEvent]
    private void OnActivate(Entity<PlanktonHarvesterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        _power.TogglePower(ent.Owner, user: args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDeviceUpdated(Entity<PlanktonHarvesterComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        if (!TryGetContainer(ent, out var container))
            return;

        if (!_nodeContainer.TryGetNode(ent.Owner, ent.Comp.InletName, out PipeNode? inlet))
            return;

        var wanted = MathF.Min(ent.Comp.MolesPerSecond * args.dt, ent.Comp.MolesPerHarvest - ent.Comp.Filtered);
        var drawn = MathF.Min(wanted, inlet.Air.GetMoles(Gas.Water));
        if (drawn <= 0f)
            return;

        inlet.Air.AdjustMoles(Gas.Water, -drawn);
        ent.Comp.Filtered += drawn;

        if (ent.Comp.Filtered < ent.Comp.MolesPerHarvest)
            return;

        ent.Comp.Filtered = 0f;
        _plankton.GenerateSpecies(container.Value, ent.Comp.SpeciesPerHarvest);
        _power.SetPowerDisabled(ent.Owner, true);
        _popup.PopupEntity(Loc.GetString("plankton-harvester-done"), ent);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<PlanktonHarvesterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetContainer(ent, out _))
        {
            args.PushMarkup(Loc.GetString("plankton-harvester-examine-no-container"));
            return;
        }

        var percent = (int) MathF.Round(ent.Comp.Filtered / ent.Comp.MolesPerHarvest * 100f);
        args.PushMarkup(Loc.GetString("plankton-harvester-examine-progress", ("percent", percent)));
    }

    private bool TryGetContainer(Entity<PlanktonHarvesterComponent> ent, [NotNullWhen(true)] out Entity<PlanktonComponent>? container)
    {
        container = null;
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.ContainerSlotId) is not { } item
            || !TryComp<PlanktonComponent>(item, out var plankton))
            return false;

        container = (item, plankton);
        return true;
    }
}
