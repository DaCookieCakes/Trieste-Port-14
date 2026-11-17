namespace Content.Server._TP.Power.WirelessPower;

/// <summary>
///     A wireless power device. Transmitters take power and <see cref="WirelessPowerSystem"/>
///         moves that charge to every anchored receiver on the same channel.
/// </summary>
[RegisterComponent, Access(typeof(WirelessPowerSystem))]
public sealed partial class WirelessPowerComponent : Component
{
    /// <summary>
    ///     Whether this device sends or receives power.
    /// </summary>
    [DataField(required: true)]
    public WirelessPowerMode Mode;

    /// <summary>
    ///     The channel ID. Devices only link to devices on the same channel.
    ///     Randomized on map init when <see cref="RandomizeChannel"/> is set, so freshly built devices don't link by accident.
    /// </summary>
    [DataField]
    public int Channel;

    /// <summary>
    ///     If true, picks a random channel on map init.
    /// </summary>
    [DataField]
    public bool RandomizeChannel = true;

    /// <summary>
    ///     Highest channel number that can be set.
    /// </summary>
    [DataField]
    public int MaxChannel = 999;

    /// <summary>
    ///     The most energy that can be received/transferred.
    /// </summary>
    [DataField]
    public float MaxTransferRate = 200000f;

    /// <summary>
    ///     The fraction of extra energy lost on top of what's delivered, with a single receiver.
    ///     0.1 means delivering 100 kW drains 110 kW from the transmitter.
    /// </summary>
    /// <remarks>Transmitter only</remarks>
    [DataField]
    public float BaseLoss = 0.1f;

    /// <summary>
    ///     The extra loss added for every anchored device on the channel beyond one receiver.
    /// </summary>
    /// <remarks>Transmitter only</remarks>
    [DataField]
    public float LossPerExtraDevice = 0.05f;

    /// <summary>
    ///     The flat power upkeep that's drained from the battery while anchored/linked.
    /// </summary>
    /// <remarks>Transmitter only</remarks>
    [DataField]
    public float IdleDraw = 1000f;

    /// <summary>
    ///     How many linked devices were on the same network.
    /// </summary>
    [ViewVariables]
    public int LinkedCount;

    /// <summary>
    ///     Power sent or received during the last update.
    /// </summary>
    [ViewVariables]
    public float LastTransfer;

    /// <summary>
    ///    The power that's been delivered / drained during the last update, as a fraction.
    /// </summary>
    /// <remarks>Transmitter only</remarks>
    [ViewVariables]
    public float LastEfficiency = 1f;
}

public enum WirelessPowerMode : byte
{
    Transmitter,
    Receiver,
}
