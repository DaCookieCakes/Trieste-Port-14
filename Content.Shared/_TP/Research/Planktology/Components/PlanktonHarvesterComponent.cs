using Robust.Shared.GameStates;

namespace Content.Shared._TP.Research.Planktology.Components;

/// <summary>
///     A machine that draws sea water from its inlet pipe while powered,
///         and filters plankton colonies out of it into an inserted plankton container.
///     It switches itself off after each harvest.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlanktonHarvesterComponent : Component
{
    [DataField]
    public string InletName = "pipe";

    [DataField]
    public string ContainerSlotId = "container";

    /// <summary>
    ///     Moles of sea water that must be filtered for one harvest.
    /// </summary>
    [DataField]
    public float MolesPerHarvest = 200f;

    /// <summary>
    ///     The most sea water the harvester can draw from its inlet per second.
    /// </summary>
    [DataField]
    public float MolesPerSecond = 20f;

    /// <summary>
    ///     How many colonies each harvest produces.
    /// </summary>
    [DataField]
    public int SpeciesPerHarvest = 3;

    /// <summary>
    ///     Sea water filtered so far towards the current harvest.
    /// </summary>
    [DataField]
    public float Filtered;
}
