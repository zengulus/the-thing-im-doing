using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Relics;

public static class RelicDefinitionCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, RelicDefinition>> Definitions = new(LoadDefinitions);

    public static IEnumerable<RelicDefinition> All => Definitions.Value.Values;

    public static RelicDefinition Get(string id)
    {
        return Definitions.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out RelicDefinition? definition)
    {
        return Definitions.Value.TryGetValue(id, out definition);
    }

    private static IReadOnlyDictionary<string, RelicDefinition> LoadDefinitions()
    {
        var definitions = new Dictionary<string, RelicDefinition>(StringComparer.Ordinal);

        foreach (RelicContentDefinition contentDefinition in ContentJsonLoader.LoadItems<RelicContentFile, RelicContentDefinition>(
                     "relics.json",
                     file => file.Relics))
        {
            definitions[contentDefinition.Id] = new RelicDefinition(
                contentDefinition.Id,
                contentDefinition.DisplayNameKey,
                contentDefinition.DescriptionKey,
                contentDefinition.Hooks
                    .Select(hook => new RelicHookDefinition(hook.Trigger, hook.BehaviorId))
                    .ToArray(),
                contentDefinition.Tags);
        }

        return definitions
            .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private sealed class RelicContentFile
    {
        public int SchemaVersion { get; set; }
        public List<RelicContentDefinition> Relics { get; set; } = [];
    }

    private sealed class RelicContentDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public List<RelicHookContentDefinition> Hooks { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    private sealed class RelicHookContentDefinition
    {
        public string Trigger { get; set; } = "";
        public string BehaviorId { get; set; } = "";
    }
}
