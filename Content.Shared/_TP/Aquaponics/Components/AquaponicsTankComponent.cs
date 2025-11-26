using Content.Shared._TP.Aquaponics.Data;

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
    public List<FishData> CurrentFish = new();

    /// <summary>
    ///     The value of how many fish can be stored in a tank.
    ///     The Default is 2.
    /// </summary>
    [DataField]
    public int MaxFish = 2;

    /// <summary>
    ///     Whether the tank can hold plants along-side fish.
    /// </summary>
    [DataField]
    public bool HoldsPlants;

    [DataField]
    public float DiseaseLevel;
}
