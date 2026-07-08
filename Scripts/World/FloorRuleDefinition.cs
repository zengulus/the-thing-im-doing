using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.World;

public sealed record FloorRuleDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    string? PushCollisionBehaviorId,
    IReadOnlyList<string> Tags)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Description => GameStrings.Get(DescriptionKey);
}

public static class FloorRuleDefinitionCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, FloorRuleDefinition>> Definitions = new(LoadDefinitions);

    public static IEnumerable<FloorRuleDefinition> All => Definitions.Value.Values;

    public static FloorRuleDefinition Get(string id)
    {
        return Definitions.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out FloorRuleDefinition? definition)
    {
        return Definitions.Value.TryGetValue(id, out definition);
    }

    private static IReadOnlyDictionary<string, FloorRuleDefinition> LoadDefinitions()
    {
        var definitions = new Dictionary<string, FloorRuleDefinition>(StringComparer.Ordinal);

        foreach (FloorRuleContentDefinition contentDefinition in ContentJsonLoader.LoadItems<FloorRuleContentFile, FloorRuleContentDefinition>(
                     "rules.json",
                     file => file.Rules))
        {
            definitions[contentDefinition.Id] = new FloorRuleDefinition(
                contentDefinition.Id,
                contentDefinition.DisplayNameKey,
                contentDefinition.DescriptionKey,
                string.IsNullOrWhiteSpace(contentDefinition.PushCollisionBehaviorId) ? null : contentDefinition.PushCollisionBehaviorId,
                contentDefinition.Tags);
        }

        return definitions
            .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private sealed class FloorRuleContentFile
    {
        public int SchemaVersion { get; set; }
        public List<FloorRuleContentDefinition> Rules { get; set; } = [];
    }

    private sealed class FloorRuleContentDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public string? PushCollisionBehaviorId { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
