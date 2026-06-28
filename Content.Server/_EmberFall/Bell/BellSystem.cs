using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._EmberFall.Bell.Components;
using Content.Shared._EmberFall.Bell.Systems;
using Content.Shared._TriestePort.Maps;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Tiles;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server._EmberFall.Bell;

/// <summary>
///     The server-side system handling the Bell.
///     Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission.
///     Heavily modified by Trieste Port.
/// </summary>
public sealed partial class BellSystem : SharedBellSystem
{
    [Dependency] private BellConsoleSystem _console = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private ShuttleSystem _shuttle = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BellComponent, FTLTagEvent>(OnShuttleTag);
        SubscribeLocalEvent<BellComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<BellComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<ShuttleComponent>(ent.Owner, out var shuttle))
            shuttle.FTLCooldownOverride = TimeSpan.FromSeconds(10);
    }

    private void OnShuttleTag(Entity<BellComponent> ent, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting.
        args.Handled = true;
        args.Tag = "TP14TagDockBell";
    }

    /// <summary>
    ///     Update method for the Bell System.
    ///     This is messy, but it has forced my hand.
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var bellQuery = EntityQueryEnumerator<BellComponent>();
        while (bellQuery.MoveNext(out var uid, out var bell))
        {
            // Refresh the destinations if we're missing any.
            // Yes this is terrible, but Robust is worse. It's forced my hand.
            var mapQuery = EntityQueryEnumerator<FTLDestinationComponent, MapComponent>();
            while (mapQuery.MoveNext(out var mapUid, out var dest, out var map))
            {
                if (!dest.Enabled)
                    continue;

                if (bell.Destinations.Any(d => d.Map == map.MapId))
                    continue;

                bell.Destinations.Add(new BellDestination
                {
                    Name = Name(mapUid),
                    Map = map.MapId,
                });

                Dirty(uid, bell);
                _console.UpdateConsolesUsing(uid);
            }

            // FTL state checks.
            var currentState = TryComp<FTLComponent>(uid, out var ftl)
                ? ftl.State
                : FTLState.Available;

            if (bell.LastFTLState != currentState)
            {
                bell.LastFTLState = currentState;
                _console.UpdateConsolesUsing(uid);
            }
        }
    }
}
