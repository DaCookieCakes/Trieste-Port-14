using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Aquaponics.Prototypes;

[Prototype("FishGene")]
public sealed class FishGenePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField(required: true)]
    public string Description { get; private set; } = default!;

    [DataField("instability")]
    public int InstabilityCost = 10;

    [DataField("incompatibilities")]
    public List<string> IncompatibleGenes = new();
}
