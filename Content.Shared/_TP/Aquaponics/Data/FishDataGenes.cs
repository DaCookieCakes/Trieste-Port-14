using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Aquaponics.Data;

public sealed class FishData
{
    [DataField]
    public string SpeciesId = string.Empty;

    [DataField]
    public float Health = 100;

    public FishGrowthStage GrowthStage = FishGrowthStage.Egg;

    /// <summary>
    ///     The genes the fish currently has.
    /// </summary>
    public List<GeneInstance> Genes = new();

    /// <summary>
    ///     The current genetic instability, default starts at 0.
    /// </summary>
    [DataField]
    public int GeneticInstability;

    [DataField]
    public float Age;

    [DataField]
    public bool IsDead;

    [DataField]
    public ProtoId<ReagentPrototype>  ProducingReagent = "Ammonia";
}

public enum FishGrowthStage
{
    Egg,
    Fry,
    Juvenile,
    Adult,
    Elder,
}

[DataDefinition]
public sealed partial class GeneInstance
{
    [DataField("ID", required: true)]
    public string ID { get; private set; } = default!;

    [DataField]
    public float Strength = 1.0f;
}
