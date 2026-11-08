using Content.Shared._TP.Research.Planktology.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Research.Planktology;

[Serializable, NetSerializable]
public enum PlanktonTankUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PlanktonTankBoundUserInterfaceState(
    List<PlanktonSpecies> species,
    bool powered,
    bool lightsOn,
    float temperature,
    float minTemperature,
    float maxTemperature,
    TimeSpan nextRadiationFlash,
    string? beakerName,
    FixedPoint2 beakerVolume,
    FixedPoint2 beakerMaxVolume,
    FixedPoint2 tankVolume,
    FixedPoint2 tankMaxVolume,
    bool hasContainer,
    int containerSpeciesCount,
    int maxSpecies,
    float minimumSubmitSize,
    bool hasServer) : BoundUserInterfaceState
{
    public readonly List<PlanktonSpecies> Species = species;
    public readonly bool Powered = powered;
    public readonly bool LightsOn = lightsOn;
    public readonly float Temperature = temperature;
    public readonly float MinTemperature = minTemperature;
    public readonly float MaxTemperature = maxTemperature;
    public readonly TimeSpan NextRadiationFlash = nextRadiationFlash;
    public readonly string? BeakerName = beakerName;
    public readonly FixedPoint2 BeakerVolume = beakerVolume;
    public readonly FixedPoint2 BeakerMaxVolume = beakerMaxVolume;
    public readonly FixedPoint2 TankVolume = tankVolume;
    public readonly FixedPoint2 TankMaxVolume = tankMaxVolume;
    public readonly bool HasContainer = hasContainer;
    public readonly int ContainerSpeciesCount = containerSpeciesCount;
    public readonly int MaxSpecies = maxSpecies;
    public readonly float MinimumSubmitSize = minimumSubmitSize;
    public readonly bool HasServer = hasServer;
}

#region Controls

[Serializable, NetSerializable]
public sealed class PlanktonTankToggleLightsMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PlanktonTankAdjustTemperatureMessage(bool raise) : BoundUserInterfaceMessage
{
    public readonly bool Raise = raise;
}

[Serializable, NetSerializable]
public sealed class PlanktonTankRadiationFlashMessage : BoundUserInterfaceMessage;

/// <summary>
///     Pour some of the inserted beaker into the tank.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonTankInjectMessage(int amount) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
}

/// <summary>
///     Pour the inserted container's colonies into the tank.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonTankDumpMessage : BoundUserInterfaceMessage;

/// <summary>
///     Scoop every colony in the tank into the inserted container.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonTankExtractMessage : BoundUserInterfaceMessage;

#endregion

#region Research form

/// <summary>
///     Base for messages about one species on the form. The number guards against a stale UI editing the wrong species.
/// </summary>
[Serializable, NetSerializable]
public abstract class PlanktonTankSpeciesMessage(int index, int number) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
    public readonly int Number = number;
}

/// <summary>
///     Picks a diet, or clears it if it's already the guess.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonTankGuessDietMessage(int index, int number, ProtoId<PlanktonDietPrototype> diet)
    : PlanktonTankSpeciesMessage(index, number)
{
    public readonly ProtoId<PlanktonDietPrototype> Diet = diet;
}

/// <summary>
///     Adds a characteristic to the guesses, or removes it if it's already there.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonTankGuessCharacteristicMessage(int index, int number, ProtoId<PlanktonCharacteristicPrototype> characteristic)
    : PlanktonTankSpeciesMessage(index, number)
{
    public readonly ProtoId<PlanktonCharacteristicPrototype> Characteristic = characteristic;
}

/// <summary>
///     Toggles a diet or characteristic as ruled out.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlanktonTankCrossOutMessage(int index, int number, string id) : PlanktonTankSpeciesMessage(index, number)
{
    public readonly string Id = id;
}

[Serializable, NetSerializable]
public sealed class PlanktonTankSubmitMessage(int index, int number) : PlanktonTankSpeciesMessage(index, number);

#endregion
