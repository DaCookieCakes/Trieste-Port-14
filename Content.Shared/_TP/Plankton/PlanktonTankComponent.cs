using Robust.Shared.Audio;

namespace Content.Shared._TP.Plankton;

/// <summary>
/// A stationary aquarium tank for cultivating plankton species.
/// Requires power and maintains temperature for optimal growth.
/// </summary>
[RegisterComponent]
public sealed partial class PlanktonTankComponent : Component
{
    /// <summary>
    ///     Maximum number of different species this tank can hold simultaneously.
    /// </summary>
    [DataField]
    public int MaxSpecies = 2;

    /// <summary>
    ///     Current temperature of the tank in Celsius.
    /// </summary>
    [DataField]
    public float CurrentTemperature = 20f;

    /// <summary>
    ///     Target temperature the tank is trying to reach.
    /// </summary>
    [DataField]
    public float TargetTemperature = 20f;

    /// <summary>
    ///     Minimum temperature the tank can be set to.
    /// </summary>
    [DataField]
    public float MinTemperature = -10f;

    /// <summary>
    ///     Maximum temperature the tank can be set to.
    /// </summary>
    [DataField]
    public float MaxTemperature = 60f;

    /// <summary>
    ///     How fast the tank heats up per second when powered.
    /// </summary>
    [DataField]
    public float HeatingRate = 0.5f;

    /// <summary>
    ///     How fast the tank cools down per second when powered.
    /// </summary>
    [DataField]
    public float CoolingRate = 0.5f;

    /// <summary>
    ///     Temperature change per verb interaction (Increase/Decrease Heat buttons).
    /// </summary>
    [DataField]
    public float TemperatureStep = 5f;

    /// <summary>
    ///     Power consumption when actively heating or cooling.
    /// </summary>
    [DataField]
    public float ActivePowerConsumption = 1000f;

    /// <summary>
    ///     Power consumption when idle.
    /// </summary>
    [DataField]
    public float IdlePowerConsumption = 200f;

    /// <summary>
    ///     Name of the solution container that holds the seawater.
    /// </summary>
    [DataField]
    public string WaterSolutionName = "tank";

    /// <summary>
    ///     Name of the solution container that holds the feeding inputs.
    /// </summary>
    [DataField]
    public string SolutionName = "input";

    /// <summary>
    ///     Minimum amount of liquid required for the plankton to survive.
    /// </summary>
    [DataField]
    public float MinimumSeawaterVolume = 50f;

    /// <summary>
    /// Whether the tank is currently powered and functional.
    /// </summary>
    [ViewVariables]
    public bool IsPowered = false;

    [ViewVariables]
    public bool LightEnabled = false;

    /// <summary>
    ///     Sound played when adjusting temperature.
    /// </summary>
    [DataField]
    public SoundSpecifier AdjustSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>
    ///     Sound played when extracting species.
    /// </summary>
    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/Machines/windoor_open.ogg");
}
