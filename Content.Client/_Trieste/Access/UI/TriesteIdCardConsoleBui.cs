using Content.Shared._Trieste.Access.Components;
using Content.Shared._Trieste.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using static Content.Shared._Trieste.Access.Components.TriesteIdCardConsoleComponent;

namespace Content.Client._Trieste.Access.UI
{
    public sealed partial class TriesteIdCardConsoleBui : BoundUserInterface
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        private readonly SharedTriesteIdCardConsoleSystem _idCardConsoleSystem = default!;

        private TriesteIdCardConsoleWindow? _window;

        public TriesteIdCardConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _idCardConsoleSystem = EntMan.System<SharedTriesteIdCardConsoleSystem>();
        }

        protected override void Open()
        {
            base.Open();
            List<ProtoId<AccessLevelPrototype>> accessLevels;

            if (EntMan.TryGetComponent<TriesteIdCardConsoleComponent>(Owner, out var idCard))
            {
                accessLevels = idCard.AccessLevels;
            }
            else
            {
                accessLevels = new List<ProtoId<AccessLevelPrototype>>();
                _idCardConsoleSystem.Log.Error($"No IdCardConsole component found for {EntMan.ToPrettyString(Owner)}!");
            }

            _window = new TriesteIdCardConsoleWindow(this, _prototypeManager, accessLevels)
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName
            };

            _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
            _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));

            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (TriesteIdCardConsoleBoundUserInterfaceState) state;
            _window?.UpdateState(castState);
        }

        public void SubmitData(string newFullName, string newJobTitle, List<ProtoId<AccessLevelPrototype>> newAccessList, ProtoId<JobPrototype>? newJobPrototype)
        {
            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newAccessList,
                newJobPrototype));
        }
    }
}
