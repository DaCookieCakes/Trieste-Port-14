using Content.Shared._TP.Aquaponics.Data;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Aquaponics.Components;

/// <summary>
///     Turns an entity into an aquaponics tank.
/// </summary>
[RegisterComponent]
public sealed partial class AquaponicsTankComponent : Component
{
    /// <summary>
    ///     The current fish species in the tank.
    /// </summary>
    public readonly List<FishData> CurrentFish = new();

    /// <summary>
    ///     The currently contained eggs in the tank
    /// </summary>
    public EntProtoId? ContainedEggId = null;

    /// <summary>
    ///     The value of how many fish can be stored in a tank.
    ///     The Default is 1.
    /// </summary>
    [DataField]
    public int MaxFish = 1;

    /// <summary>
    ///     Whether the tank can hold plants along-side fish.
    /// </summary>
    [DataField]
    public bool HoldsPlants;

    [DataField]
    public float DiseaseLevel;
}

[Serializable, NetSerializable]
public enum AquaponicsTankVisuals : byte
{
    Beaker,
    LightAlert,
    LightFood,
    LightHarvest,
    LightHealth,
    LightWaste,
    FishStageOne,
    FishStageTwo,
    FishStageThree,
    FishStageFour,
}
