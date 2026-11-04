using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Research.Planktology.Prototypes;

/// <summary>
///     What a randomly generated plankton species eats. Each species rolls exactly one.
/// </summary>
[Prototype]
public sealed partial class PlanktonDietPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    ///     Relative chance of rolling this diet.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    ///     How the species feeds. <see cref="PlanktonDietBehavior.None"/> means it can't feed yet.
    /// </summary>
    [DataField]
    public PlanktonDietBehavior Behavior = PlanktonDietBehavior.None;

    /// <summary>
    ///     For <see cref="PlanktonDietBehavior.Reagents"/>: any of these in the tank's water count as food.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> Foods = new();
}

public enum PlanktonDietBehavior : byte
{
    /// <summary>
    ///     The species will starve.
    /// </summary>
    None,

    /// <summary>
    ///     Eats other living species in the same sample.
    /// </summary>
    Carnivore,

    /// <summary>
    ///     Feeds in a lit, powered tank.
    /// </summary>
    Photosynthetic,

    /// <summary>
    ///     Feeds on radiation instead of being hurt by it.
    /// </summary>
    Radiophage,

    /// <summary>
    ///     Feeds in a powered tank, raising its power draw.
    /// </summary>
    Electrophage,

    /// <summary>
    ///     Eats dead plankton.
    /// </summary>
    Decomposer,

    /// <summary>
    ///     Eats any of <see cref="PlanktonDietPrototype.Foods"/> from the tank's water.
    /// </summary>
    Reagents,
}
