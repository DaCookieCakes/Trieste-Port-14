using Robust.Shared.Serialization;

namespace Content.Shared._TP.Research.Planktology;

/// <summary>
///     A single randomly generated plankton species living inside a <see cref="Components.PlanktonComponent"/> sample.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class PlanktonSpecies
{
    /// <summary>
    ///     Randomly generated scientific name, e.g. "Volvox aurelia".
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    [DataField]
    public PlanktonDiet Diet;

    [DataField]
    public PlanktonCharacteristics Characteristics;

    /// <summary>
    ///     The culture size. Grows while the species is well-fed.
    /// </summary>
    [DataField]
    public float Size = 1f;

    /// <summary>
    /// Satiation. The species starves to death when this hits zero.
    /// </summary>
    [DataField]
    public float Hunger;

    [DataField]
    public bool Alive = true;

    public bool Has(PlanktonCharacteristics characteristic)
    {
        return (Characteristics & characteristic) != 0;
    }
}

[Serializable, NetSerializable]
public enum PlanktonDiet : byte
{
    Carnivore,
    Photosynthetic,
    Radiophage,
    Saguinophage,
    Electrophage,
    Symbiotroph,
    Chemophage,
    Decomposer,
    Scavenger,
}

[Flags, Serializable, NetSerializable]
public enum PlanktonCharacteristics
{
    None = 0,
    Aggressive = 1 << 0,
    Bioluminescent = 1 << 1,
    Parasitic = 1 << 2,
    Radioactive = 1 << 3,
    Charged = 1 << 4,
    Pyrotechnic = 1 << 5,
    Mimicry = 1 << 6,
    ChemicalProduction = 1 << 7,
    MagneticField = 1 << 8,
    Hallucinogenic = 1 << 9,
    PheromoneGlands = 1 << 10,
    PolypColony = 1 << 11,
    AerosolSpores = 1 << 12,
    HyperExoticSpecies = 1 << 13,
    Sentience = 1 << 14,
    Pyrophilic = 1 << 15,
    Cryophilic = 1 << 16,
}
