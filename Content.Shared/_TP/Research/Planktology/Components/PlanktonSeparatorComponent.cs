using Robust.Shared.GameStates;

namespace Content.Shared._TP.Research.Planktology.Components;

/// <summary>
///     A cryogenic filter. Plankton deposited here are frozen (their <see cref="PlanktonComponent.Stasis"/> is set),
///         and can be pulled out into an inserted plankton container.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlanktonSeparatorComponent : Component
{
    [DataField]
    public string ContainerSlotId = "container";
}
