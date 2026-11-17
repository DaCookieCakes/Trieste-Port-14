using Robust.Shared.Serialization;

namespace Content.Shared._TP.Power.WirelessPower;

/// <summary>
///     Appearance keys the for wireless power entities.
/// </summary>
[Serializable, NetSerializable]
public enum WirelessPowerVisuals : byte
{
    /// <summary>
    ///     Whether the device is linked to at least one device of the opposite mode.
    /// </summary>
    Linked,
}
