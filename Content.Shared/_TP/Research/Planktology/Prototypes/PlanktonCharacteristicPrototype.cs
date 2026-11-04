using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Research.Planktology.Prototypes;

/// <summary>
///     A trait a randomly generated plankton species can roll, like bioluminescence or aggression.
///     A species never rolls the same characteristic twice.
/// </summary>
[Prototype]
public sealed partial class PlanktonCharacteristicPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    ///     How much this characteristic is worth when a species is analyzed.
    /// </summary>
    [DataField]
    public int Points = 1;

    /// <summary>
    ///     Relative chance of rolling this characteristic.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    ///     Characteristics that can't appear alongside this one. Works both ways,
    ///     so only one side of a pair needs to list the other.
    /// </summary>
    [DataField]
    public List<ProtoId<PlanktonCharacteristicPrototype>> Conflicts = new();

    /// <summary>
    ///     Hardcoded simulation behavior, if any.
    /// </summary>
    [DataField]
    public PlanktonCharacteristicBehavior Behavior = PlanktonCharacteristicBehavior.None;

    /// <summary>
    ///     Overrides the lowest tank temperature (K) the species is comfortable at.
    /// </summary>
    [DataField]
    public float? MinComfortTemperature;

    /// <summary>
    ///     Overrides the highest tank temperature (K) the species is comfortable at.
    /// </summary>
    [DataField]
    public float? MaxComfortTemperature;
}

public enum PlanktonCharacteristicBehavior : byte
{
    None,

    /// <summary>
    ///     Shrinks every other living species in the same sample.
    /// </summary>
    Aggressive,
}
