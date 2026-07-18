using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Containers;
using Content.Server.StationRecords.Systems;
using Content.Shared._Trieste.Access.Components;
using Content.Shared._Trieste.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Construction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using static Content.Shared._Trieste.Access.Components.TriesteIdCardConsoleComponent;

namespace Content.Server._Trieste.Access.Systems;

[UsedImplicitly]
public sealed partial class TriesteIdCardConsoleSystem : SharedTriesteIdCardConsoleSystem
{
    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private StationRecordsSystem _record = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private AccessSystem _access = default!;
    [Dependency] private IdCardSystem _idCard = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriesteIdCardConsoleComponent, WriteToTargetIdMessage>(OnWriteToTargetIdMessage);

        // one day, maybe bound user interfaces can be shared too.
        SubscribeLocalEvent<TriesteIdCardConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<TriesteIdCardConsoleComponent, EntInsertedIntoContainerMessage>(OnIdInserted);
        SubscribeLocalEvent<TriesteIdCardConsoleComponent, EntRemovedFromContainerMessage>(OnIdRemoved);
        SubscribeLocalEvent<TriesteIdCardConsoleComponent, DamageChangedEvent>(OnDamageChanged);

        // Intercept the event before anyone can do anything with it!
        SubscribeLocalEvent<TriesteIdCardConsoleComponent, MachineDeconstructedEvent>(OnMachineDeconstructed,
            before: [typeof(EmptyOnMachineDeconstructSystem), typeof(ItemSlotsSystem)]);
    }

    /// <summary>
    /// <para>Handles removing the IDs, as well as saving all changes when removed.</para>
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnIdRemoved(EntityUid uid, TriesteIdCardConsoleComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == TargetIdCardSlotId)
        {
            component.TargetIdAccessSnapshot.Clear();
            component.TargetIdNameSnapshot = string.Empty;
        }

        UpdateUserInterface(uid, component, args);
    }

    /// <summary>
    /// <para>Handles inserting an ID, as well as snapshotting the target ID.</para>
    /// </summary>
    /// <param name="uid">Target UID</param>
    /// <param name="component">IDCardConsole Component</param>
    /// <param name="args">EntInsertedIntoContainerMessage Argument</param>
    private void OnIdInserted(EntityUid uid, TriesteIdCardConsoleComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == TargetIdCardSlotId)
        {

            var targetId = args.Entity;
            component.TargetIdAccessSnapshot = _access.TryGetTags(targetId)?.ToList()
                                               ?? new List<ProtoId<AccessLevelPrototype>>();
            component.TargetIdNameSnapshot = Comp<MetaDataComponent>(targetId).EntityName;
        }

        UpdateUserInterface(uid, component, args);
    }

    private void OnWriteToTargetIdMessage(EntityUid uid, TriesteIdCardConsoleComponent component, WriteToTargetIdMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        TryWriteToTargetId(uid, args.FullName, args.JobTitle, args.AccessList, args.JobPrototype, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void UpdateUserInterface(EntityUid uid, TriesteIdCardConsoleComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        EntityUid? privilegedIdEntity = null;
        var privilegedIdName = string.Empty;
        List<ProtoId<AccessLevelPrototype>>? possibleAccess = null;
        if (component.PrivilegedIdSlot.Item is { Valid: true } item)
        {
            privilegedIdEntity = item;
            privilegedIdName = Comp<MetaDataComponent>(item).EntityName;
            possibleAccess = _accessReader.FindAccessTags(item).ToList();
        }

        TriesteIdCardConsoleBoundUserInterfaceState newState;
        // this could be prettier
        if (component.TargetIdSlot.Item is not { Valid: true } targetId)
        {
            newState = new TriesteIdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component, out _),
                false,
                null,
                null,
                null,
                possibleAccess,
                string.Empty,
                privilegedIdName,
                string.Empty,
                privilegedIdEntity != null ? GetNetEntity(privilegedIdEntity.Value) : null,
                null);
        }
        else
        {
            var targetIdComponent = Comp<IdCardComponent>(targetId);
            var targetAccessComponent = Comp<AccessComponent>(targetId);

            var jobProto = targetIdComponent.JobPrototype ?? new ProtoId<JobPrototype>(string.Empty);
            if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && keyStorage.Key is { } key
                && _record.TryGetRecord<GeneralStationRecord>(key, out var record))
            {
                jobProto = record.JobPrototype;
            }

            newState = new TriesteIdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component, out _),
                true,
                targetIdComponent.FullName,
                targetIdComponent.LocalizedJobTitle,
                targetAccessComponent.Tags.ToList(),
                possibleAccess,
                jobProto,
                privilegedIdName,
                Name(targetId),
                privilegedIdEntity != null ? GetNetEntity(privilegedIdEntity.Value) : null,
                GetNetEntity(targetId));
        }

        _userInterface.SetUiState(uid, TriesteIdCardConsoleUiKey.Key, newState);
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access from the target ID card.
    /// Writes data passed from the UI into the ID stored in <see cref="TriesteIdCardConsoleComponent.TargetIdSlot"/>, if present.
    /// </summary>
    private void TryWriteToTargetId(EntityUid uid,
        string newFullName,
        string newJobTitle,
        List<ProtoId<AccessLevelPrototype>> newAccessList,
        ProtoId<JobPrototype>? newJobProto,
        EntityUid player,
        TriesteIdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, component, out var privilegedId))
            return;

        // Limit name and job title lengths
        var maxNameLength = _cfgManager.GetCVar(CCVars.MaxNameLength);
        var maxIdJobLength = _cfgManager.GetCVar(CCVars.MaxIdJobLength);

        if (newFullName.Length > maxNameLength)
            newFullName = newFullName[..maxNameLength];

        if (newJobTitle.Length > maxIdJobLength)
            newJobTitle = newJobTitle[..maxIdJobLength];

        _idCard.TryChangeFullName(targetId, newFullName, player: player);
        _idCard.TryChangeJobTitle(targetId, newJobTitle, player: player);

        if (ProtoMan.TryIndex(newJobProto, out var job)
            && ProtoMan.Resolve(job.Icon, out var jobIcon))
        {
            _idCard.TryChangeJobIcon(targetId, jobIcon, player: player);
            _idCard.TryChangeJobDepartment(targetId, job);
        }

        UpdateStationRecord(uid, targetId, newFullName, newJobTitle, job);
        if ((!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out _))
            && newJobProto != string.Empty)
        {
            Comp<IdCardComponent>(targetId).JobPrototype = newJobProto;
        }

        if (!newAccessList.All(component.AccessLevels.Contains))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
            return;
        }

        var oldTags = _access.TryGetTags(targetId)?.ToList() ?? new List<ProtoId<AccessLevelPrototype>>();

        if (oldTags.SequenceEqual(newAccessList))
            return;

        // Sets for the requested changes to the access card.
        var addedTags = newAccessList.Except(oldTags).ToList();
        var removedTags = oldTags.Except(newAccessList).ToList();
        var changedTags = addedTags.Union(removedTags).ToList();

        // Find tags that the console changed and knew about.
        var visibleChanges = changedTags.Intersect(component.AccessLevels);
        // Find tags that the original ID had that the console can't change.
        var hiddenTags = oldTags.Except(component.AccessLevels);

        var privilegedPerms = _accessReader.FindAccessTags(privilegedId.Value);
        if (!visibleChanges.All(privilegedPerms.Contains))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to modify permissions they could not give/take!");
            return;
        }

        // Restore all hidden tags to the newly requested set.
        newAccessList.AddRange(hiddenTags);
        _access.TrySetTags(targetId, newAccessList);

        var changeStrings = addedTags.Select(tag => "+" + tag) // All added tags.
            .Concat(removedTags.Except(newAccessList).Select(tag => "-" + tag)); // All removed tags (except new set due to hidden tags)

        // ECS SharedIdCardConsoleComponent and then log on card ejection, together with the save.
        // This current implementation is pretty shit as it logs 27 entries (27 lines) if someone decides to give themselves
        _adminLogger.Add(LogType.Action,
            $"{player} has modified {targetId} with the following accesses: [{string.Join(", ", changeStrings)}] [{string.Join(", ", newAccessList)}]");
    }

    /// <summary>
    /// Returns true if there is an ID in <see cref="TriesteIdCardConsoleComponent.PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReaderComponent"/>.
    /// </summary>
    private bool PrivilegedIdIsAuthorized(EntityUid uid, TriesteIdCardConsoleComponent component, [NotNullWhen(true)] out EntityUid? id)
    {
        id = null;
        if (component.PrivilegedIdSlot.Item == null)
            return false;

        id = component.PrivilegedIdSlot.Item;
        if (!TryComp<AccessReaderComponent>(uid, out var reader))
            return true;

        return _accessReader.IsAllowed(id.Value, uid, reader);
    }

    private void UpdateStationRecord(EntityUid uid, EntityUid targetId, string newFullName, ProtoId<AccessLevelPrototype> newJobTitle, JobPrototype? newJobProto)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.Name = newFullName;
        record.JobTitle = newJobTitle;

        if (newJobProto != null)
        {
            record.JobPrototype = newJobProto.ID;
            record.JobIcon = newJobProto.Icon;
        }

        _record.Synchronize(key);
    }

    private void OnMachineDeconstructed(Entity<TriesteIdCardConsoleComponent> entity, ref MachineDeconstructedEvent args)
    {
        TryDropAndThrowIds(entity.AsNullable());
    }

    private void OnDamageChanged(Entity<TriesteIdCardConsoleComponent> entity, ref DamageChangedEvent args)
    {
        if (TryDropAndThrowIds(entity.AsNullable()))
            _chat.TrySendInGameICMessage(entity, Loc.GetString("id-card-console-damaged"), InGameICChatType.Speak, true);
    }

    #region PublicAPI

    /// <summary>
    ///     Tries to drop any IDs stored in the console, and then tries to throw them away.
    ///     Returns true if anything was ejected and false otherwise.
    /// </summary>
    public bool TryDropAndThrowIds(Entity<TriesteIdCardConsoleComponent?, ItemSlotsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        var didEject = false;

        foreach (var slot in ent.Comp2.Slots.Values)
        {
            if (slot.Item == null || slot.ContainerSlot == null)
                continue;

            var item = slot.Item.Value;
            if (_container.Remove(item, slot.ContainerSlot))
            {
                _throwing.TryThrow(item, _random.NextVector2(), baseThrowSpeed: 5f);
                didEject = true;
            }
        }

        return didEject;
    }

    #endregion
}
