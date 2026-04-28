using System.Linq;
using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

// !! TRIESTE PORT MODIFIED !! //
[RegisterComponent, Access(typeof(SliceableFoodSystem))]
public sealed partial class SliceableFoodComponent : Component
{
    /// <summary>
    ///     !! TRIESTE PORT OVERHAUL !!
    ///     Prototype (and now count) to spawn after slicing.
    ///     If null then it can't be sliced.
    /// </summary>
    [DataField]
    public List<SliceResult> Slice = new();

    /// <summary>
    ///     !! TRIESTE PORT OVERHAUL !!
    ///     Number of slices the food starts with.
    /// </summary>
    public int TotalCount => Slice.Sum(s => s.Count);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    ///     How long it takes for this food to be sliced
    /// </summary>
    [DataField]
    public float SliceTime = 1f;

    /// <summary>
    ///     All the pieces will be shifted in random directions.
    /// </summary>
    [DataField]
    public float SpawnOffset = 0.5f;
}

/// <summary>
///     !! TRIESTE SPECIFIC !!
///     A struct for the spawned prototypes.
/// </summary>
[DataDefinition]
public partial struct SliceResult()
{
    [DataField] public EntProtoId Proto = default;
    [DataField] public int Count = 5;
}
