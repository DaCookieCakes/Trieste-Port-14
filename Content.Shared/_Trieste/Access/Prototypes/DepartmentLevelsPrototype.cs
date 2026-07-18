using Content.Shared.Access;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trieste.Access.Prototypes;

[Prototype]
public sealed partial class DepartmentLevelsPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ProtoId<AccessLevelPrototype> AccessLevel { get; set; }

    /// <summary>
    ///     TRIESTE SPECIFIC
    ///     The department(s) this access level belongs to, for display in the ID card console.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> Departments { get; set; } = new();
}
