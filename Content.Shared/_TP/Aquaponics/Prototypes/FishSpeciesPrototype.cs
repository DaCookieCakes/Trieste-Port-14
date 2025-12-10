using Content.Shared._TP.Aquaponics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._TP.Aquaponics.Prototypes;

/// <summary>
///     The prototype for the produced fish species from eggs.
///     See <see cref="FishEggComponent"/> for more information.
/// </summary>
[Prototype("fish")]
public sealed class FishSpeciesPrototype : IPrototype
{
    /// <summary>
    ///     The ID of the species prototype.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of the species.
    ///     This is used to name the eggs, tank examine, and the fish itself.
    /// </summary>
    [DataField(required: true)]
   public string Name { get; private set; } = default!;

    /// <summary>
    ///     The fish species.
    ///     There are five in total, see <see cref="FishSpecies"/>.
    /// </summary>
    [DataField(required: true)]
    public FishSpecies Species { get; private set; } = FishSpecies.Pisciform;

    /// <summary>
    ///     The tier of the fish.
    ///     This is typically tier 1-4, but we may add more later.
    /// </summary>
    [DataField("tier")]
    public int FishTier = 1;

    #region Fish Genes
    /// <summary>
    ///     A list of base genes this species has.
    ///     This is used by the tank and DNA systems.
    /// </summary>
    [DataField("baseGenes")]
    public List<string> BaseFishGenes = new();

    [DataField("lockedGenes")]
    public List<string> LockedFishGenes = new();
    #endregion

    [DataField("harvested", required: true)]
    public EntProtoId HarvestedItemId { get; private set; }

    [DataField("sprites", required: true)]
    public List<SpriteSpecifier> SpriteSpecifier { get; private set; } = default!;

    [DataField("eggID")]
    public EntProtoId? ProducingEggId;
}

public enum FishSpecies
{
    Aberrant,
    Crustacean,
    JellidType,
    Mutant,
    Pisciform,
}
