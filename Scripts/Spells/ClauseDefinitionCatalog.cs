using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TheThingImDoing.Spells;

public static class ClauseDefinitionCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, ClauseDefinition>> Definitions = new(LoadDefinitions);

    public static IEnumerable<ClauseDefinition> All => Definitions.Value.Values;

    public static ClauseDefinition Get(string id)
    {
        return Definitions.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out ClauseDefinition? definition)
    {
        return Definitions.Value.TryGetValue(id, out definition);
    }

    private static IReadOnlyDictionary<string, ClauseDefinition> LoadDefinitions()
    {
        var definitions = new Dictionary<string, ClauseDefinition>(StringComparer.Ordinal);

        foreach (ClauseContentDefinition contentDefinition in Content.ContentJsonLoader.LoadItems<ClauseContentFile, ClauseContentDefinition>(
                     "clauses.json",
                     file => file.Clauses))
        {
            if (!Enum.TryParse(contentDefinition.Family, ignoreCase: true, out ClauseFamily family))
            {
                continue;
            }

            definitions[contentDefinition.Id] = new ClauseDefinition(
                contentDefinition.Id,
                contentDefinition.DisplayNameKey,
                contentDefinition.PlayerTextKey,
                family,
                contentDefinition.CounterCosts,
                contentDefinition.CounterGains,
                contentDefinition.TooltipKey,
                contentDefinition.BehaviorId,
                contentDefinition.IsCondition,
                contentDefinition.Tags);
        }

        return definitions
            .OrderBy(pair => pair.Value.Family)
            .ThenBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private sealed class ClauseContentFile
    {
        public int SchemaVersion { get; set; }
        public List<ClauseContentDefinition> Clauses { get; set; } = [];
    }

    private sealed class ClauseContentDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string PlayerTextKey { get; set; } = "";
        public string TooltipKey { get; set; } = "";
        public string Family { get; set; } = "";
        public Dictionary<string, int> CounterCosts { get; set; } = [];
        public Dictionary<string, int> CounterGains { get; set; } = [];
        public string BehaviorId { get; set; } = "";
        public bool IsCondition { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
