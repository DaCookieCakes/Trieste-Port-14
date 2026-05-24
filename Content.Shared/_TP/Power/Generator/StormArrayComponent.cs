using Robust.Shared.Serialization;

namespace Content.Shared._TP.Power.Generator;

/// <summary>
///     Component for the storm array.
///     Created by Cookie for Trieste Port 14.
/// </summary>
[RegisterComponent]
public sealed partial class StormArrayComponent : Component
{
    [DataField]
    public bool Enabled;

    #region Announcements

    [DataField]
    public bool FirstAnnouncement;

    [DataField]
    public bool SecondAnnouncement;

    [DataField]
    public bool ThirdAnnouncement;

    [DataField]
    public bool FourthAnnouncement;

    [DataField]
    public bool FifthAnnouncement;

    public string StatusMessage;

    #endregion

    #region Cooling

    /// <summary>
    ///     How many joules per second the array generates from the storm.
    /// </summary>
    [DataField]
    public float HeatGenerationRate = 25000f;

    /// <summary>
    ///     Heat capacity for internal temperature calculations.
    /// </summary>
    [DataField]
    public float SelfHeatCapacity = 5000f;

    [DataField]
    public float CoolingEfficiency = 0.8F;

    #endregion

    #region AtmosStorage

    [ViewVariables]
    public float LastCoolingRate = 0.0F;

    [ViewVariables]
    public float LastCoolantFlow = 0.0F;

    #endregion

}

[Serializable, NetSerializable]
public enum StormArrayVisuals : byte
{
    Idle,
    Active,
}
