using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._TriestePort.Jellids;

public sealed partial class SharedJellidSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;

    /// <summary>
    ///
    /// </summary>
    /// <param name="target"></param>
    public void DefibJellid(EntityUid target)
    {
        if (!TryComp<BatteryComponent>(target, out var battery))
            return;

        // If the target has a battery (Jellids), restores some of their internal energy.
        // This will heal Jellids and prevent instantly dying again.
        const float batteryAdd = 150f;
        var newCharge = _battery.GetCharge((target, battery)) + batteryAdd;

        _battery.SetCharge(target, newCharge);
        Log.Info($"Added {batteryAdd} charge to {target} battery. New charge: {newCharge}");
    }
}
