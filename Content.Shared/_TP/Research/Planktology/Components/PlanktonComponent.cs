using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._TP.Research.Planktology.Components;

/// <summary>
///     A container holding one or more plankton species that eat, grow, fight and die over time.
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

    /// <summary>
    ///     Frozen plankton don't eat, fight, grow or starve.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Stasis;

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

    /// <summary>
    ///     Food a photosynthetic species gets per interaction tick in a lit, powered tank.
    /// </summary>
    [DataField]
    public float PhotosynthesisRate = 0.15f;

    /// <summary>
    ///     Food an electrophage gets per interaction tick in a powered tank.
    ///     Electrophages make the tank draw more power instead, see <see cref="PlanktonTankComponent"/>.
    /// </summary>
    [DataField]
    public float ElectrophageRate = 0.15f;

    /// <summary>
    ///     Units of reagent a chemophage or hemophage eats from the tank per interaction tick.
    /// </summary>
    [DataField]
    public FixedPoint2 ReagentEatRate = 0.1;

    /// <summary>
    ///     Food gained per unit of reagent eaten.
    /// </summary>
    [DataField]
    public float ReagentFoodValue = 1.5f;

    /// <summary>
    ///     Lowest tank temperature (K) a species is comfortable at, unless a characteristic overrides it.
    /// </summary>
    [DataField]
    public float MinComfortTemperature = 283.15f;

    /// <summary>
    ///     Highest tank temperature (K) a species is comfortable at, unless a characteristic overrides it.
    /// </summary>
    [DataField]
    public float MaxComfortTemperature = 303.15f;

    /// <summary>
    ///     Size lost per interaction tick by a species that is uncomfortable with the tank temperature.
    /// </summary>
    [DataField]
    public float TemperatureDamage = 0.1f;

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
