using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._TriestePort.Jellids;

[RegisterComponent]
public sealed partial class JellidComponent : Component
{
    /// <summary>
    ///     How much percentage to drain from a held battery when used.
    ///     This is calculated against the battery's max charge.
    /// </summary>
    [DataField]
    public float DrainPercent = 0.05F;

    /// <summary>
    ///     The next time the Jellid power drain will be called.
    /// </summary>
    [DataField]
    public TimeSpan NextPowerDrain = TimeSpan.Zero;

    /// <summary>
    ///     The normal Jellid charge alert.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "TP14AlertJellidBattery";

    /// <summary>
    ///     The alert to play when the Jellid's internal charge is low empty.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "TP14AlertJellidBatteryNone";

    public SoundPathSpecifier BatteryUseSound = new("/Audio/_TriestePort/Items/jellid_battery_use.ogg");
}

[Serializable, NetSerializable]
public sealed partial class JellidBatteryDoAfterEvent : SimpleDoAfterEvent;
