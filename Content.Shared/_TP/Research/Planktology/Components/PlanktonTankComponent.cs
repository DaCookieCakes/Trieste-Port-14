using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._TP.Research.Planktology.Components;

/// <summary>
///     A culture tank with controls.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PlanktonTankComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool LightsOn;

    /// <summary>
    ///     The most colonies the tank holds at once.
    /// </summary>
    [DataField]
    public int MaxSpecies = 3;

    [DataField]
    public string ContainerSlotId = "container";

    [DataField]
    public string BeakerSlotId = "beaker";

    /// <summary>
    ///     A colony must be at least this big before its research form can be submitted.
    /// </summary>
    [DataField]
    public float MinimumSubmitSize = 50f;

    /// <summary>
    ///     Research points for a correct form, before characteristic points.
    /// </summary>
    [DataField]
    public int BaseResearchPoints = 1000;

    /// <summary>
    ///     Research points per point of the species' characteristics, for a correct form.
    /// </summary>
    [DataField]
    public int ResearchPointsPerCharacteristicPoint = 750;

    /// <summary>
    ///     How often an open UI is refreshed so sizes and the radiation timer stay current.
    /// </summary>
    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextUiUpdate;

    /// <summary>
    ///     The tank's water, where chemophages and hemophages find their food.
    /// </summary>
    [DataField]
    public string SolutionName = "tank";

    /// <summary>
    ///     Power the tank draws with no electrophages in it.
    /// </summary>
    [DataField]
    public float BasePowerLoad = 500f;

    /// <summary>
    ///     Extra power drawn per point of living electrophage culture size.
    /// </summary>
    [DataField]
    public float ElectrophagePowerPerSize = 10f;

    /// <summary>
    ///     Cap on the extra power electrophages can draw.
    /// </summary>
    [DataField]
    public float MaxElectrophagePower = 1000f;

    /// <summary>
    ///     Water temperature in Kelvin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Temperature = 293.15f;

    [DataField]
    public float TemperatureStep = 10f;

    [DataField]
    public float MinTemperature = 223.15f;

    [DataField]
    public float MaxTemperature = 373.15f;

    /// <summary>
    ///     Rads delivered by one radiation flash.
    /// </summary>
    [DataField]
    public float RadiationFlashDose = 5f;

    [DataField]
    public TimeSpan RadiationFlashCooldown = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRadiationFlash;
}
