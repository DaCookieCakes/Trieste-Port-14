using Content.Client.Examine;
using Content.Shared._TP.Power.Generator;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._TP.Power.Generator;

/// <summary>
///     Client-side logic for the storm array.
///     This is essentially a carbon-copy of the TEG Circulator system.
///     Created by Cookie for Trieste Port 14.
/// </summary>
public sealed class StormArraySystem : EntitySystem
{
    private static readonly EntProtoId ArrowPrototype = "TP14StormArrayCirculatorArrow";

    public override void Initialize()
    {
        SubscribeLocalEvent<StormArrayComponent, ClientExaminedEvent>(OnExaminedEvent);
    }

    private void OnExaminedEvent(Entity<StormArrayComponent> arrayEnt, ref ClientExaminedEvent args)
    {
        Spawn(ArrowPrototype, new EntityCoordinates(arrayEnt.Owner, 0, 0));
    }
}
