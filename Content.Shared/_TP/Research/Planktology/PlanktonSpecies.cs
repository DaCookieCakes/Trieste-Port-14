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

    /// <summary>
    ///     Random 3 digit tag shown on the species' bubble in the culture tank.
    /// </summary>
    [DataField]
    public int Number;

    #region Research form

    // The research form's notes live on the species, so they follow it between tanks and the separator.

    /// <summary>
    ///     The diet the researchers think this species has.
    /// </summary>
    [DataField]
    public ProtoId<PlanktonDietPrototype>? GuessedDiet;

    /// <summary>
    ///     The characteristics the researchers think this species has, at most three.
    /// </summary>
    [DataField]
    public List<ProtoId<PlanktonCharacteristicPrototype>> GuessedCharacteristics = new();

    /// <summary>
    ///     Diets and characteristics the researchers have ruled out.
    /// </summary>
    [DataField]
    public HashSet<string> CrossedOut = new();

    /// <summary>
    ///     Whether the form has been submitted. Each species can only be submitted once.
    /// </summary>
    [DataField]
    public bool Submitted;

    /// <summary>
    ///     Whether the submitted form was correct.
    /// </summary>
    [DataField]
    public bool SubmittedCorrect;

    #endregion

    public bool Has(ProtoId<PlanktonCharacteristicPrototype> characteristic)
    {
        return Characteristics.Contains(characteristic);
    }
}
