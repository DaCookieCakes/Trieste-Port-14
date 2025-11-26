using Content.Shared._TP.Aquaponics.Data;

namespace Content.Shared._TP.Aquaponics.Components;

[RegisterComponent]
public sealed partial class GeneticFishComponent : Component
{
    /// <summary>
    ///     The species of this fish.
    /// </summary>
    [DataField("speciesID")]
    public string SpeciesId = string.Empty;

    /// <summary>
    ///     The genes this entity currently has.
    /// </summary>
    [DataField]
    public List<GeneInstance> Genes = new();

    /// <summary>
    ///     The genetic instability of this fish.
    /// </summary>
    [DataField]
    public int GeneticInstability;
}
