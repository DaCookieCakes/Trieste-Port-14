using Content.Shared._TP.Power.Passive;
using Robust.Client.GameObjects;

namespace Content.Client._TP.Power.Passive;

public sealed class StormMastVisualizerSystem : VisualizerSystem<StormMastComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, StormMastComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;


        // Idle //
        if (!AppearanceSystem.TryGetData<bool>(uid, StormMastVisuals.Idle, out var idle, args.Component))
            idle = true;

        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), StormMastVisuals.Idle, out var idleLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), idleLayer, idle);


        // Active //
        if (!AppearanceSystem.TryGetData<bool>(uid, StormMastVisuals.Active, out var active, args.Component))
           active = false;

        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), StormMastVisuals.Active, out var activeLayer, false))
           SpriteSystem.LayerSetVisible((uid, args.Sprite), activeLayer, active);
    }
}
