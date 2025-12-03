using Content.Shared._TP.Aquaponics.Components;
using Robust.Client.GameObjects;

namespace Content.Client._TP14.Aquaponics;

public sealed class AquaponicsTankVisualizerSystem : VisualizerSystem<AquaponicsTankComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AquaponicsTankComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;


    }
}
