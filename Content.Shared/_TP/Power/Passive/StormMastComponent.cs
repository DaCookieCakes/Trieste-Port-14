using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Power.Passive;

/// <summary>
///     Part of the Engi overhaul for Trieste.
///     Must be activated along-side Superconducting Coils to run the Storm Array.
/// </summary>
[RegisterComponent]
public sealed partial class StormMastComponent : Component
{
    /// <summary>
    ///     Whether the Storm Mast is enabled.
    ///     All Storm Masts on the same grid as the Storm Array must be enabled, or power output dampens.
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    ///     Whether the Storm Mast is damaged.
    ///     If damaged an engineer must apply Steel sheets and a welder, then reactivate it.
    /// </summary>
    [DataField]
    public bool Damaged;

    /// <summary>
    ///     Whether the Storm Mast is 'patched'.
    ///     Intermediate between repaired and damaged. Appears when Steel is applied, but before a welder is used.
    /// </summary>
    [DataField]
    public bool Patched;
}

[Serializable, NetSerializable]
public enum StormMastVisuals : byte
{
    Idle,
    Active,
}

[Serializable, NetSerializable]
public sealed partial class StormMastEnableDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StormMastApplySteelDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StormMastWeldDoAfter : SimpleDoAfterEvent;
