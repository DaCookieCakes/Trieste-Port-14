using Content.Shared._TP.Research.Planktology;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._TP.Research.Planktology.UI;

[UsedImplicitly]
public sealed class PlanktonTankBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PlanktonTankWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PlanktonTankWindow>();

        _window.OnInject += amount => SendMessage(new PlanktonTankInjectMessage(amount));
        _window.OnDump += () => SendMessage(new PlanktonTankDumpMessage());
        _window.OnExtract += () => SendMessage(new PlanktonTankExtractMessage());
        _window.OnToggleLights += () => SendMessage(new PlanktonTankToggleLightsMessage());
        _window.OnAdjustTemperature += raise => SendMessage(new PlanktonTankAdjustTemperatureMessage(raise));
        _window.OnRadiationFlash += () => SendMessage(new PlanktonTankRadiationFlashMessage());

        _window.OnGuessDiet += (index, number, diet) => SendMessage(new PlanktonTankGuessDietMessage(index, number, diet));
        _window.OnGuessCharacteristic += (index, number, characteristic) =>
            SendMessage(new PlanktonTankGuessCharacteristicMessage(index, number, characteristic));
        _window.OnCrossOut += (index, number, id) => SendMessage(new PlanktonTankCrossOutMessage(index, number, id));
        _window.OnSubmit += (index, number) => SendMessage(new PlanktonTankSubmitMessage(index, number));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PlanktonTankBoundUserInterfaceState tankState)
            _window?.UpdateState(tankState);
    }
}
