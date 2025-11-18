using Content.Server.BPL.Pinata;
using Content.Shared.Body.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._BPL.Pinata;

/*
The MIT License (MIT)

Copyright © 2025 .cerol (Discord)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

public sealed class PinataSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinataComponent, DamageModifyEvent>(OnHit);
        SubscribeLocalEvent<PinataComponent, BeingGibbedEvent>(OnGib);
        SubscribeLocalEvent<PinataComponent, EntityGibbedEvent>(OnGibAlt);
    }

    //This is from most explicit gib effects
    private void OnGibAlt(Entity<PinataComponent> ent, ref EntityGibbedEvent args)
    {
        var guts = args.DroppedEntities;
        foreach (var organ in guts)
        {
            QueueDel(organ);
        }
        args.DroppedEntities.Clear();

        var coords = Transform(ent).Coordinates;
        for (int i = 0; i < _random.Next(12, 21); i++)
        {
            SpawnItem(ent);
        }
    }

    //This is from taking too much damage and gibbing.
    private void OnGib(Entity<PinataComponent> ent, ref BeingGibbedEvent args)
    {
        var guts = args.GibbedParts;
        foreach (var organ in guts)
        {
            QueueDel(organ);
        }
        args.GibbedParts.Clear();

        var coords = Transform(ent).Coordinates;
        for (int i = 0; i < _random.Next(12, 21); i++)
        {
            SpawnItem(ent);
        }
    }

    private void OnHit(Entity<PinataComponent> ent, ref DamageModifyEvent args)
    {
        var damPerGroup = args.Damage.GetDamagePerGroup(_proto);
        if (damPerGroup.TryGetValue("Brute", out var brute) && brute > 5) //Has to be a decent hit
        {
            for (int i = 0; i < _random.Next(ent.Comp.MinSpawn, ent.Comp.MaxSpawn); i++)
            {
                SpawnItem(ent);
            }
        }
    }

    public void SpawnItem(Entity<PinataComponent> entity)
    {
        var coords = Transform(entity).Coordinates;
        var newCandy = Spawn(entity.Comp.SpawnOnHit, coords);
        var physics = EnsureComp<PhysicsComponent>(newCandy);

        var targetMapVelocity = _random.NextVector2().Normalized() * _random.Next(8, 20);
        var currentMapVelocity = Physics.GetMapLinearVelocity(newCandy, physics);
        var finalLinear = physics.LinearVelocity + targetMapVelocity - currentMapVelocity;
        Physics.SetLinearVelocity(newCandy, finalLinear, body: physics);
        TransformSystem.SetWorldRotation(newCandy, _random.NextVector2().ToWorldAngle());
    }
}
