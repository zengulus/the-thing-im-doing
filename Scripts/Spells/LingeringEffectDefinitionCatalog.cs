using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public static class LingeringEffectDefinitionCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, LingeringEffectDefinition>> Definitions = new(LoadDefinitions);

    public static LingeringEffectDefinition Get(string id)
    {
        return Definitions.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out LingeringEffectDefinition? definition)
    {
        return Definitions.Value.TryGetValue(id, out definition);
    }

    private static IReadOnlyDictionary<string, LingeringEffectDefinition> LoadDefinitions()
    {
        var definitions = new Dictionary<string, LingeringEffectDefinition>(StringComparer.Ordinal);

        foreach (LingeringEffectContentDefinition contentDefinition in ContentJsonLoader
                     .LoadItems<LingeringEffectContentFile, LingeringEffectContentDefinition>(
                         "lingering_effects.json",
                         file => file.LingeringEffects))
        {
            definitions[contentDefinition.Id] = new LingeringEffectDefinition(
                contentDefinition.Id,
                contentDefinition.DisplayNameKey,
                contentDefinition.OnApplyBehaviorId,
                contentDefinition.OnTurnStartBehaviorId,
                contentDefinition.OnMoveBehaviorId,
                contentDefinition.OnDeathBehaviorId,
                contentDefinition.OnActorBecameAdjacentBehaviorId,
                contentDefinition.OnBeforeDamageBehaviorId,
                contentDefinition.Counters.ToHashSet(StringComparer.Ordinal));
        }

        return definitions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private sealed class LingeringEffectContentFile
    {
        public int SchemaVersion { get; set; }
        public List<LingeringEffectContentDefinition> LingeringEffects { get; set; } = [];
    }

    private sealed class LingeringEffectContentDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string OnApplyBehaviorId { get; set; } = "";
        public string OnTurnStartBehaviorId { get; set; } = "";
        public string OnMoveBehaviorId { get; set; } = "";
        public string OnDeathBehaviorId { get; set; } = "";
        public string OnActorBecameAdjacentBehaviorId { get; set; } = "";
        public string OnBeforeDamageBehaviorId { get; set; } = "";
        public List<string> Counters { get; set; } = [];
    }
}
