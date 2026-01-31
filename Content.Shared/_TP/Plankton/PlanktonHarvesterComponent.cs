using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._TP.Plankton;

/// <summary>
/// Filters and harvests plankton from SeaWater gas.
/// Functions similarly to a gas condenser but produces plankton cultures instead of reagents.
/// </summary>
[RegisterComponent]
public sealed partial class PlanktonHarvesterComponent : Component
{
    /// <summary>
    /// Minimum amount of seawater gas required per harvest cycle.
    /// </summary>
    [DataField]
    public float SeaWaterRequired = 10f;

    /// <summary>
    /// How long between harvest attempts in seconds.
    /// </summary>
    [DataField]
    public float HarvestInterval = 60f;

    /// <summary>
    /// Next time the harvester can attempt to harvest plankton.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextHarvestTime = TimeSpan.Zero;

    /// <summary>
    /// Minimum number of plankton species to generate per harvest.
    /// </summary>
    [DataField]
    public int MinSpecies = 1;

    /// <summary>
    /// Maximum number of plankton species to generate per harvest.
    /// </summary>
    [DataField]
    public int MaxSpecies = 3;

    /// <summary>
    /// Slot ID for the plankton sample container.
    /// </summary>
    [DataField]
    public string ContainerSlotId = "plankton_container_slot";

    /// <summary>
    /// Power consumption when actively harvesting.
    /// </summary>
    [DataField]
    public float ActivePowerConsumption = 10000f;

    /// <summary>
    /// Power consumption when idle.
    /// </summary>
    [DataField]
    public float IdlePowerConsumption = 500f;

    /// <summary>
    /// Sound played when harvesting completes.
    /// </summary>
    [DataField]
    public SoundSpecifier HarvestSound = new SoundPathSpecifier("/Audio/Machines/chime.ogg");

    /// <summary>
    /// Sound played when harvesting fails.
    /// </summary>
    [DataField("failSound")]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    /// <summary>
    /// Whether the harvester is currently powered and operational.
    /// </summary>
    [ViewVariables]
    public bool IsPowered = false;

    /// <summary>
    /// Whether a harvest is currently in progress.
    /// </summary>
    [ViewVariables]
    public bool IsHarvesting = false;
}
