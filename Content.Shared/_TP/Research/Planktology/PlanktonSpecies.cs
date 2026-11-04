using Content.Shared._TP.Research.Planktology.Prototypes;
using Robust.Shared.Prototypes;
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

    [DataField(required: true)]
    public ProtoId<PlanktonDietPrototype> Diet;

    /// <summary>
    ///     A list of the current characteristics on a species.
    ///     Used to prevent calling the same ones twice.
    /// </summary>
    [DataField]
    public List<ProtoId<PlanktonCharacteristicPrototype>> Characteristics = new();

    /// <summary>
    ///     The culture size. Grows while the species is well-fed.
    /// </summary>
    [DataField]
    public float Size = 1f;

    /// <summary>
    ///     The species will starve to death when this hits zero.
    /// </summary>
    [DataField]
    public float Hunger;

    [DataField]
    public bool Alive = true;

    public bool Has(ProtoId<PlanktonCharacteristicPrototype> characteristic)
    {
        return Characteristics.Contains(characteristic);
    }
}
