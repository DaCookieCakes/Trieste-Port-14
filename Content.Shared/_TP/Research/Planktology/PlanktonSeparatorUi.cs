using Robust.Shared.Serialization;

namespace Content.Shared._TP.Research.Planktology;

[Serializable, NetSerializable]
public enum PlanktonSeparatorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PlanktonSeparatorBoundUserInterfaceState(
    List<PlanktonSeparatorEntry> frozen,
    bool hasContainer,
    int containerSpeciesCount) : BoundUserInterfaceState
{
    public readonly List<PlanktonSeparatorEntry> Frozen = frozen;
    public readonly bool HasContainer = hasContainer;
    public readonly int ContainerSpeciesCount = containerSpeciesCount;
}

[Serializable, NetSerializable]
public readonly record struct PlanktonSeparatorEntry(int Index, string Name, bool Alive, float Size);

/// <summary>
///     Move everything in the inserted container into the separator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonSeparatorDepositMessage : BoundUserInterfaceMessage;

/// <summary>
///     Move one frozen species into the inserted container.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonSeparatorExtractMessage(int index, string name) : BoundUserInterfaceMessage
{
    public readonly int Index = index;

    /// <summary>
    ///     Sent alongside the index so a stale UI can't pull out the wrong species.
    /// </summary>
    public readonly string Name = name;
}
