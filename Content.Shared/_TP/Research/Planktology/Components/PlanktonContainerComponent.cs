using Robust.Shared.GameStates;

namespace Content.Shared._TP.Research.Planktology.Components;

/// <summary>
///     Marks an entity as a portable plankton carrier. 
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlanktonContainerComponent : Component;
