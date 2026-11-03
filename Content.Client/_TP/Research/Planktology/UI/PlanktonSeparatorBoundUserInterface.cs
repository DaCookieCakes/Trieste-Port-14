using Content.Shared._TP.Research.Planktology;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._TP.Research.Planktology.UI;

[UsedImplicitly]
public sealed class PlanktonSeparatorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PlanktonSeparatorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PlanktonSeparatorWindow>();
        _window.OnDeposit += () => SendMessage(new PlanktonSeparatorDepositMessage());
        _window.OnExtract += entry => SendMessage(new PlanktonSeparatorExtractMessage(entry.Index, entry.Name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PlanktonSeparatorBoundUserInterfaceState separatorState)
            _window?.UpdateState(separatorState);
    }
}
