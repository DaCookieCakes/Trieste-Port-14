using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._TriestePort.Maps;

/// <summary>
///     A component to load multiple maps along-side a main one.
/// </summary>
[RegisterComponent, ComponentProtoName("MultiMapManager")]
public sealed partial class MultiMapComponent : Component
{
    /// <summary>
    ///     A dictionary of the selected maps to load.
    ///     <para>string: What to name the station. Used by the PDA and other menus.</para>
    ///     <para>ProtoId: The "GameMap" prototype ID to load. These are NOT maps files.</para>
    /// </summary>
    [DataField]
    public Dictionary<string, ProtoId<GameMapPrototype>> Maps { get; set; } = new();
}
