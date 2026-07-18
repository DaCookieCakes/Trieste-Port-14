using Content.Shared._Trieste.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trieste.Access.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTriesteIdCardConsoleSystem))]
public sealed partial class TriesteIdCardConsoleComponent : Component
{
    public static string PrivilegedIdCardSlotId = "IdCardConsole-privilegedId";
    public static string TargetIdCardSlotId = "IdCardConsole-targetId";

    [DataField]
    public ItemSlot PrivilegedIdSlot = new();

    [DataField]
    public ItemSlot TargetIdSlot = new();

    [DataField]
    public List<ProtoId<AccessLevelPrototype>> TargetIdAccessSnapshot = new();

    [DataField]
    public string TargetIdNameSnapshot = string.Empty;

    [Serializable, NetSerializable]
    public sealed class WriteToTargetIdMessage : BoundUserInterfaceMessage
    {
        public readonly string FullName;
        public readonly string JobTitle;
        public readonly List<ProtoId<AccessLevelPrototype>> AccessList;
        public readonly ProtoId<JobPrototype>? JobPrototype;

        public WriteToTargetIdMessage(string fullName, string jobTitle, List<ProtoId<AccessLevelPrototype>> accessList, ProtoId<JobPrototype>? jobPrototype)
        {
            FullName = fullName;
            JobTitle = jobTitle;
            AccessList = accessList;
            JobPrototype = jobPrototype;
        }
    }

    // Put this on shared so we just send the state once in PVS range rather than every time the UI updates.
    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> AccessLevels = new()
    {
        "Armory",
        "Atmospherics",
        "Bar",
        "Brig",
        "Detective",
        "Cargo",
        "Chapel",
        "Chemistry",
        "ChiefEngineer",
        "ChiefMedicalOfficer",
        "Command",
        "Cryogenics",
        "Engineering",
        "Expeditions",
        "External",
        "GenpopEnter",
        "GenpopLeave",
        "Harbormaster",
        "HeadOfPersonnel",
        "Hydroponics",
        "Janitor",
        "Kitchen",
        "Lawyer",
        "Maintenance",
        "Marshal",
        "Medical",
        "Overseer",
        "Petrochem",
        "Research",
        "ResearchDirector",
        "Security",
        "Service",
        "Theatre",
    };

    [Serializable, NetSerializable]
    public sealed class TriesteIdCardConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string PrivilegedIdName;
        public readonly bool IsPrivilegedIdPresent;
        public readonly bool IsPrivilegedIdAuthorized;
        public readonly bool IsTargetIdPresent;
        public readonly string TargetIdName;
        public readonly string? TargetIdFullName;
        public readonly string? TargetIdJobTitle;
        public readonly List<ProtoId<AccessLevelPrototype>>? TargetIdAccessList;
        public readonly List<ProtoId<AccessLevelPrototype>>? AllowedModifyAccessList;
        public readonly ProtoId<JobPrototype> TargetIdJobPrototype;
        public readonly NetEntity? PrivilegedIdEntity;
        public readonly NetEntity? TargetIdEntity;

        public TriesteIdCardConsoleBoundUserInterfaceState(bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            bool isTargetIdPresent,
            string? targetIdFullName,
            string? targetIdJobTitle,
            List<ProtoId<AccessLevelPrototype>>? targetIdAccessList,
            List<ProtoId<AccessLevelPrototype>>? allowedModifyAccessList,
            ProtoId<JobPrototype> targetIdJobPrototype,
            string privilegedIdName,
            string targetIdName,
            NetEntity? privilegedIdEntity = null,
            NetEntity? targetIdEntity = null)
        {
            IsPrivilegedIdPresent = isPrivilegedIdPresent;
            IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
            IsTargetIdPresent = isTargetIdPresent;
            TargetIdFullName = targetIdFullName;
            TargetIdJobTitle = targetIdJobTitle;
            TargetIdAccessList = targetIdAccessList;
            AllowedModifyAccessList = allowedModifyAccessList;
            TargetIdJobPrototype = targetIdJobPrototype;
            PrivilegedIdName = privilegedIdName;
            TargetIdName = targetIdName;
            PrivilegedIdEntity = privilegedIdEntity;
            TargetIdEntity = targetIdEntity;
        }
    }

    [Serializable, NetSerializable]
    public enum TriesteIdCardConsoleUiKey : byte
    {
        Key,
    }
}
