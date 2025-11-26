using Content.Shared._TP.Aquaponics.Data;

namespace Content.Shared._TP.Aquaponics.Components;

[RegisterComponent]
public sealed partial class FishEggComponent : Component
{
    [DataField("speciesID", required: true)]
    public string SpeciesId = string.Empty;

    [DataField]
    public List<GeneInstance> Genes = new();

    [DataField]
    public int GeneticInstability;
}
