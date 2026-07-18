using Content.Shared._Trieste.Access.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._Trieste.Access.Systems;

[UsedImplicitly]
public abstract partial class SharedTriesteIdCardConsoleSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;

    public const string Sawmill = "idconsole";
    protected ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = LogManager.GetSawmill(Sawmill);

        SubscribeLocalEvent<TriesteIdCardConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TriesteIdCardConsoleComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, TriesteIdCardConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, TriesteIdCardConsoleComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
        _itemSlotsSystem.AddItemSlot(uid, TriesteIdCardConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
    }

    private void OnComponentRemove(EntityUid uid, TriesteIdCardConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
        _itemSlotsSystem.RemoveItemSlot(uid, component.TargetIdSlot);
    }

    [Serializable, NetSerializable]
    private sealed class IdCardConsoleComponentState : ComponentState
    {
        public List<string> AccessLevels;

        public IdCardConsoleComponentState(List<string> accessLevels)
        {
            AccessLevels = accessLevels;
        }
    }
}
