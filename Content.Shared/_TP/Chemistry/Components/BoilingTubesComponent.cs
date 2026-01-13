using  Robust.Shared.Audio;

namespace Content.Shared._TP.Chemistry.Components;

[RegisterComponent]
public sealed partial class BoilingTubesComponent : Component
{
    [DataField]
    public bool Toggled;

    [DataField]
    public SoundPathSpecifier? ToggledSound = new("/Audio/Effects/internals.ogg");
}
