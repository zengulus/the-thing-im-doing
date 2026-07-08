using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public static class EffectDefinitionCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, EffectDefinition>> Definitions = new(LoadDefinitions);

    public static EffectDefinition Get(string id)
    {
        return Definitions.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out EffectDefinition? definition)
    {
        return Definitions.Value.TryGetValue(id, out definition);
    }

    private static IReadOnlyDictionary<string, EffectDefinition> LoadDefinitions()
    {
        var definitions = new Dictionary<string, EffectDefinition>(StringComparer.Ordinal);

        foreach (EffectContentDefinition contentDefinition in ContentJsonLoader
                     .LoadItems<EffectContentFile, EffectContentDefinition>(
                         "effects.json",
                         file => file.Effects))
        {
            definitions[contentDefinition.Id] = new EffectDefinition(
                contentDefinition.Id,
                contentDefinition.DisplayNameKey,
                contentDefinition.Triggers
                    .Select(trigger => new EffectTriggerDefinition(
                        trigger.TriggerId,
                        trigger.BehaviorId,
                        trigger.Priority))
                    .ToArray(),
                contentDefinition.Counters.ToHashSet(StringComparer.Ordinal));
        }

        return definitions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private sealed class EffectContentFile
    {
        public int SchemaVersion { get; set; }
        public List<EffectContentDefinition> Effects { get; set; } = [];
    }

    private sealed class EffectContentDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public List<EffectTriggerContentDefinition> Triggers { get; set; } = [];
        public List<string> Counters { get; set; } = [];
    }

    private sealed class EffectTriggerContentDefinition
    {
        public string TriggerId { get; set; } = "";
        public string BehaviorId { get; set; } = "";
        public int Priority { get; set; }
    }
}
