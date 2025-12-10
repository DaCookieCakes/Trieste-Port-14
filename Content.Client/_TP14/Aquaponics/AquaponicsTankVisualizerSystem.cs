using Content.Shared._TP.Aquaponics.Components;
using Robust.Client.GameObjects;

namespace Content.Client._TP14.Aquaponics;

public sealed class AquaponicsTankVisualizerSystem : VisualizerSystem<AquaponicsTankComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AquaponicsTankComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.Beaker, out var beaker, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.Beaker, out var beakerLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), beakerLayer, beaker);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.LightAlert, out var alert, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.LightAlert, out var alertLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), alertLayer, alert);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.LightFood, out var food, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.LightFood, out var foodLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), foodLayer, food);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.LightHarvest, out var harvest, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.LightHarvest, out var harvestLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), harvestLayer, harvest);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.LightHealth, out var health, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.LightHealth, out var healthLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), healthLayer, health);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.LightWaste, out var waste, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.LightWaste, out var wasteLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), wasteLayer, waste);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.FishStageOne, out var fishOne, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.FishStageOne, out var fishOneLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), fishOneLayer, fishOne);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.FishStageTwo, out var fishTwo, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.FishStageTwo, out var fishTwoLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), fishTwoLayer, fishTwo);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.FishStageThree, out var fishThree, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.FishStageThree, out var fishThreeLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), fishThreeLayer, fishThree);

        AppearanceSystem.TryGetData<bool>(uid, AquaponicsTankVisuals.FishStageFour, out var fishFour, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), AquaponicsTankVisuals.FishStageFour, out var fishFourLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), fishFourLayer, fishFour);
    }
}
