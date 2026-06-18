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
    public int CurrentTemperature = 1;

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
