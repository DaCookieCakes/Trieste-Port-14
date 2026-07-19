using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server._Trieste.Maps;

/// <summary>
///     A copy of <see cref="MapMigrationSystem"/> for Trieste specific migrations.
///     Performs basic map migration operations by listening for engine <see cref="MapLoaderSystem"/> events.
/// </summary>
public sealed partial class TriesteMigrationSystem : EntitySystem
{
    [Dependency] private IResourceManager _resMan = default!;

    private const string MigrationFile = "/trieste-migration.yml";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeEntityReadEvent>(OnBeforeEntitiesRead);

        #if DEBUG
        if (!TryReadFile(out var mappings))
            return;

        // Verify that all of the entries map to valid entity prototypes.
        foreach (var node in mappings.Children.Values)
        {
            var newId = ((ValueDataNode)node).Value;
            if (!string.IsNullOrEmpty(newId) && newId != "null")
                DebugTools.Assert(ProtoMan.HasIndex<EntityPrototype>(newId), $"{newId} is not an entity prototype.");
        }
        #endif
    }

    private bool TryReadFile([NotNullWhen(true)] out MappingDataNode? mappings)
    {
        mappings = null;
        var path = new ResPath(MigrationFile);
        if (!_resMan.TryContentFileRead(path, out var stream))
            return false;

        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var documents = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();

        if (documents == null)
            return false;

        mappings = (MappingDataNode) documents.Root;
        return true;
    }

    private void OnBeforeEntitiesRead(BeforeEntityReadEvent ev)
    {
        if (!TryReadFile(out var mappings))
            return;

        foreach (var (key, val) in mappings)
        {
            if (val is not ValueDataNode dataNode)
                continue;

            if (string.IsNullOrWhiteSpace(dataNode.Value) || dataNode.Value == "null")
                ev.DeletedPrototypes.Add(key);
            else
                ev.RenamedPrototypes.Add(key, dataNode.Value);
        }
    }
}
