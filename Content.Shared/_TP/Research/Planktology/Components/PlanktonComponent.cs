using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._TP.Research.Planktology.Components;

/// <summary>
/// A sample of water holding one or more plankton species that eat, grow, fight and die over time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PlanktonComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<PlanktonSpecies> Species = new();

    /// <summary>
    ///     The amount of dead plankton matter in the sample.
    ///     It can be food for decomposers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeadPlankton;

    /// <summary>
    ///     How many random species to generate when the sample spawns.
    /// </summary>
    [DataField]
    public int InitialSpecies;

    [DataField]
    public ProtoId<DatasetPrototype> FirstNames = "TP14PlanktonFirstNames";

    [DataField]
    public ProtoId<DatasetPrototype> SecondNames = "TP14PlanktonSecondNames";

    #region Timing

    /// <summary>
    ///     How often species interact with each other and feed.
    /// </summary>
    [DataField]
    public TimeSpan InteractionInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     How often species get hungry and/or grow.
    /// </summary>
    [DataField]
    public TimeSpan MetabolismInterval = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextInteraction;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMetabolism;

    #endregion

    #region Tuning

    [DataField]
    public float MaxHunger = 50f;

    /// <summary>
    ///     Flat hunger lost every metabolism tick.
    /// </summary>
    [DataField]
    public float HungerLoss = 0.5f;

    /// <summary>
    ///     Extra hunger lost per point of size every metabolism tick.
    ///     Bigger cultures eat more.
    /// </summary>
    [DataField]
    public float HungerLossPerSize = 0.01f;

    /// <summary>
    ///     The size that an aggressive species knocks off of other species in an interaction tick.
    /// </summary>
    [DataField]
    public float AggressionDamage = 0.1f;

    /// <summary>
    ///     The base size a carnivore eats from other species in an interaction tick.
    /// </summary>
    [DataField]
    public float CarnivoreDamage = 0.1f;

    /// <summary>
    ///     The extra size a carnivore eats per point of its own size.
    /// </summary>
    [DataField]
    public float CarnivoreDamagePerSize = 0.005f;

    /// <summary>
    ///     How many dead plankton a decomposer eats per interaction tick.
    /// </summary>
    [DataField]
    public float DecomposeRate = 0.2f;

    [DataField]
    public float PhotosynthesisRate = 0.001f;

    /// <summary>
    ///     The food a radiophage gets per rad absorbed.
    /// </summary>
    [DataField]
    public float RadiationFood = 0.5f;

    /// <summary>
    ///     The size every other species loses per rad absorbed.
    /// </summary>
    [DataField]
    public float RadiationDamage = 0.5f;

    #endregion
}
