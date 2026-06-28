using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared._TriestePort.Maps;
using Content.Shared.Maps;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._TriestePort.Maps;

/// <summary>
///     The multimap system for the MultiMapManager component.
/// </summary>
public sealed partial class MultiMapSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    /// <summary>
    ///     Loads multiple maps that are listed in the <see cref="MultiMapComponent"/>'s dictionary.
    /// </summary>
    /// <param name="args">RoundStartingEvent arguments</param>
    private void OnRoundStarting(RoundStartingEvent args)
    {
        var query = EntityQueryEnumerator<MultiMapComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            // We start by making a "toInitialize" list of four values.
            // This is called OUTSIDE the component's 'Maps' dictionary so they can load and init properly.
            List<(Entity<MapComponent>?, HashSet<Entity<MapGridComponent>>?, string, GameMapPrototype)> toInitialize = new();

            // Now we iterate the name and prototype IDs in the maps.
            // We try to index the prototype and export the game map prototype itself.
            // Then, we try to LOAD the map from the game map's path. Exporting an ent of it, and the grids.
            // Finally, in this section we set the map name and add it all to the "toInitialize" lists.
            foreach (var (name, protoId) in comp.Maps)
            {
                if (!_protoManager.TryIndex(protoId, out var gameMap))
                {
                    Log.Error($"MultiMapSystem: Failed to find GameMapPrototype {protoId}");
                    continue;
                }

                if (!_mapLoader.TryLoadMap(gameMap.MapPath, out var mapEnt, out var grids))
                {
                    Log.Error($"MultiMapSystem: Failed to load map {protoId}");
                    continue;
                }

                _metaData.SetEntityName(mapEnt.Value, name);
                toInitialize.Add((mapEnt, grids, name, gameMap));
            }

            // In this final section we iterate through "toInitialize".
            // Starting by checking if the maps or grids are null, we then try to initialize and unpause the maps.
            // Finally, we get the IDs from the maps, make a new "PostGameMapLoad" event, and trigger the event.
            // Triggering tells the game to initialize stuff such as jobs.
            foreach (var (mapEnt, grids, name, gameMap) in toInitialize)
            {
                if (mapEnt == null || grids == null)
                {
                    Log.Warning($"MultiMapSystem: Failed to initialize map {name}");
                    Log.Warning("This was caused by a null map or grid!");
                    continue;
                }

                _map.InitializeMap(mapEnt.Value.Owner, unpause: true);
                Log.Info($"MultiMapSystem: Loaded map {name} as {mapEnt.Value}");

                var mapId = Comp<MapComponent>(mapEnt.Value).MapId;
                var ev = new PostGameMapLoad(gameMap, mapId, grids.Select(g => g.Owner).ToList(), name);
                RaiseLocalEvent(ev);
            }
        }
    }
}
