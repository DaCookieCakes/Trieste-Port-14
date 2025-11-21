using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Chemistry.Prototypes;

[Prototype("triesteReagent")]
public sealed partial class TriesteReagentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("reagentID", required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField]
    public float BoilingPoint;

    [DataField("compoundTypes", required: true)]
    public List<CompoundType> Compounds = new();
}

[Serializable, NetSerializable]
public enum CompoundType : byte
{
    Gas,
    Liquid,
    Metal,
    Solid,
    Other,
}
