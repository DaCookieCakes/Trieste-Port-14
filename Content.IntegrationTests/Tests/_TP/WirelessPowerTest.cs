#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._TP.Power.WirelessPower;
using Content.Server.Power.Components;
using Content.Shared.Coordinates;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._TP;

/// <summary>
///     Checks that power moved by a wireless transmitter reaches a receiver on another map,
///     and stops when the channels no longer match.
/// </summary>
[TestFixture]
[TestOf(typeof(WirelessPowerSystem))]
public sealed class WirelessPowerTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TP14WirelessTestGenerator
  components:
  - type: Transform
    anchored: true
  - type: NodeContainer
    nodes:
      output:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: PowerSupplier

- type: entity
  id: TP14WirelessTestConsumer
  components:
  - type: Transform
    anchored: true
  - type: NodeContainer
    nodes:
      input:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: PowerConsumer
";

    private const float Draw = 100_000f;

    [Test]
    public async Task BeamsPowerAcrossMaps()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var timing = server.ResolveDependency<IGameTiming>();
        var mapSys = entMan.System<SharedMapSystem>();
        var xformSys = entMan.System<SharedTransformSystem>();
        var wireless = entMan.System<WirelessPowerSystem>();

        EntityUid txEnt = default;
        EntityUid rxEnt = default;
        PowerSupplierComponent supplier = default!;
        PowerConsumerComponent consumer = default!;

        await server.WaitAssertion(() =>
        {
            // Sending side: generator -> HV -> terminal -> transmitter.
            mapSys.CreateMap(out var mapA);
            var gridA = mapSys.CreateGridEntity(mapA);
            for (var i = 0; i < 3; i++)
                mapSys.SetTile(gridA, new Vector2i(0, i), new Tile(1));
            for (var i = 0; i < 2; i++)
                entMan.SpawnAttachedTo("CableHV", gridA.Owner.ToCoordinates(0, i));

            entMan.SpawnAttachedTo("CableTerminal", gridA.Owner.ToCoordinates(0, 1), rotation: Angle.FromDegrees(180));
            var genEnt = entMan.SpawnAttachedTo("TP14WirelessTestGenerator", gridA.Owner.ToCoordinates(0, 0));
            txEnt = entMan.SpawnAttachedTo("TP14WirelessPowerTransmitter", gridA.Owner.ToCoordinates(0, 2));
            xformSys.AnchorEntity(txEnt);

            supplier = entMan.GetComponent<PowerSupplierComponent>(genEnt);
            supplier.MaxSupply = 500_000;
            supplier.SupplyRampTolerance = 500_000;

            // Receiving side, on a different map: receiver -> HV -> consumer.
            mapSys.CreateMap(out var mapB);
            var gridB = mapSys.CreateGridEntity(mapB);
            for (var i = 0; i < 2; i++)
            {
                mapSys.SetTile(gridB, new Vector2i(0, i), new Tile(1));
                entMan.SpawnAttachedTo("CableHV", gridB.Owner.ToCoordinates(0, i));
            }

            rxEnt = entMan.SpawnAttachedTo("TP14WirelessPowerReceiver", gridB.Owner.ToCoordinates(0, 0));
            xformSys.AnchorEntity(rxEnt);
            var consumerEnt = entMan.SpawnAttachedTo("TP14WirelessTestConsumer", gridB.Owner.ToCoordinates(0, 1));
            consumer = entMan.GetComponent<PowerConsumerComponent>(consumerEnt);
            consumer.DrawRate = Draw;

            Assert.That(wireless.SetChannel(txEnt, 42));
            Assert.That(wireless.SetChannel(rxEnt, 42));
        });

        server.RunTicks(timing.TickRate * 6);

        await server.WaitAssertion(() =>
        {
            var tx = entMan.GetComponent<WirelessPowerComponent>(txEnt);
            var rx = entMan.GetComponent<WirelessPowerComponent>(rxEnt);

            Assert.Multiple(() =>
            {
                Assert.That(tx.LinkedCount, Is.EqualTo(1));
                Assert.That(rx.LinkedCount, Is.EqualTo(1));
                Assert.That(consumer.ReceivedPower, Is.EqualTo(Draw).Within(1f));
                Assert.That(rx.LastTransfer, Is.GreaterThan(0f));
                Assert.That(tx.LastEfficiency, Is.EqualTo(1f / (1f + tx.BaseLoss)).Within(0.001f));
            });

            // Retune the receiver away from the transmitter.
            Assert.That(wireless.SetChannel(rxEnt, 43));
            Assert.That(wireless.SetChannel(rxEnt, -1), Is.False);
        });

        // Receiver buffer (500 kJ) runs dry at 100 kW in ~5 seconds.
        server.RunTicks(timing.TickRate * 8);

        await server.WaitAssertion(() =>
        {
            var tx = entMan.GetComponent<WirelessPowerComponent>(txEnt);
            var rx = entMan.GetComponent<WirelessPowerComponent>(rxEnt);

            Assert.Multiple(() =>
            {
                Assert.That(tx.LinkedCount, Is.EqualTo(0));
                Assert.That(rx.LinkedCount, Is.EqualTo(0));
                Assert.That(consumer.ReceivedPower, Is.LessThan(1f));
            });
        });
    }
}
