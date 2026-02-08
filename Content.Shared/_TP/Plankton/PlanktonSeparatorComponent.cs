using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._TP.Plankton;

[RegisterComponent]
public sealed partial class PlanktonSeparatorComponent : Component
{
    /// <summary>
    ///     Next time the harvester can attempt to harvest plankton.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSeparatorTime = TimeSpan.Zero;

    [DataField]
    public bool Separated;

    /// <summary>
    ///     How long it takes to separate while a container is present
    /// </summary>
    [DataField]
    public float SeparateInterval = 60f;

    /// <summary>
    ///     Power consumption when idle.
    /// </summary>
    [DataField]
    public float IdlePowerConsumption = 200f;

    /// <summary>
    ///     Sound played when extracting species.
    /// </summary>
    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/Machines/windoor_open.ogg");

    /// <summary>
    ///     Sound played when separation completes.
    /// </summary>
    [DataField]
    public SoundSpecifier SeparationSound = new SoundPathSpecifier("/Audio/Machines/chime.ogg");
}
