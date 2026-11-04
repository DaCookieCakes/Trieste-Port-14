using Content.Shared._TP.Research.Planktology.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Research.Planktology.Components;

/// <summary>
/// A handheld scanner that prints a report of a plankton sample,
///     or, in analysis mode, turns a large single-species culture into a research disk.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlanktonScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool AnalysisMode;

    [DataField]
    public EntProtoId ReportPrototype = "Paper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [DataField]
    public EntProtoId RewardPrototype = "ResearchDisk5000";

    /// <summary>
    ///     The reward for analyzing a species with <see cref="AdvancedRewardCharacteristic"/>.
    /// </summary>
    [DataField]
    public EntProtoId AdvancedRewardPrototype = "ResearchDisk10000";

    [DataField]
    public ProtoId<PlanktonCharacteristicPrototype> AdvancedRewardCharacteristic = "TP14PlanktonCharacteristicHyperExotic";

    /// <summary>
    ///     The minimum culture size needed before it can be analyzed.
    /// </summary>
    [DataField]
    public float MinimumAnalysisSize = 50f;
}
