using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Power.Generator;

[Serializable, NetSerializable]
public sealed partial class StormArrayDoAfterEvent : SimpleDoAfterEvent;
