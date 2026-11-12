using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Loadouts;

/// <summary>
///     Purely cosmetic grouping of <see cref="LoadoutGroupPrototype"/>s in the lobby loadout window.
///     Each category becomes one tab, with its member groups as the nested tabs.
/// </summary>
[Prototype("loadoutCategory")]
public sealed partial class TriesteLoadoutCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    ///     The title of the tab.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    ///     Every loadout group that should be shown under this category.
    ///     Only the groups present in the role loadout are actually shown.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<LoadoutGroupPrototype>> Groups = new();

    /// <summary>
    ///     If a role only has a single group from this category, show that group as a normal
    ///         top-level tab instead of a category containing one tab.
    /// </summary>
    [DataField]
    public bool CollapseSingle = true;
}
