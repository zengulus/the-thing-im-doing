using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Behaviors;

public static class BehaviorPrimitiveCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, BehaviorPrimitiveDefinition>> Definitions = new(LoadDefinitions);

    public static IEnumerable<BehaviorPrimitiveDefinition> All => Definitions.Value.Values;

    public static BehaviorPrimitiveDefinition Get(string id)
    {
        return Definitions.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out BehaviorPrimitiveDefinition? definition)
    {
        return Definitions.Value.TryGetValue(id, out definition);
    }

    private static IReadOnlyDictionary<string, BehaviorPrimitiveDefinition> LoadDefinitions()
    {
        var definitions = new Dictionary<string, BehaviorPrimitiveDefinition>(StringComparer.Ordinal);

        foreach (BehaviorPrimitiveContentDefinition contentDefinition in ContentJsonLoader.LoadItems<BehaviorPrimitiveContentFile, BehaviorPrimitiveContentDefinition>(
                     "behavior_primitives.json",
                     file => file.Primitives))
        {
            definitions[contentDefinition.Id] = new BehaviorPrimitiveDefinition(
                contentDefinition.Id,
                contentDefinition.DisplayNameKey,
                contentDefinition.DescriptionKey,
                string.IsNullOrWhiteSpace(contentDefinition.BehaviorId) ? null : contentDefinition.BehaviorId,
                contentDefinition.Scopes,
                contentDefinition.Tags);
        }

        return definitions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private sealed class BehaviorPrimitiveContentFile
    {
        public int SchemaVersion { get; set; }
        public List<BehaviorPrimitiveContentDefinition> Primitives { get; set; } = [];
    }

    private sealed class BehaviorPrimitiveContentDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public string? BehaviorId { get; set; }
        public List<string> Scopes { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }
}
