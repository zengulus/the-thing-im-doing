using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;
using TheThingImDoing.Spells;
using TheThingImDoing.World;

namespace TheThingImDoing.Relics;

public static class RelicDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<RelicDefinition>> Registry = new(LoadRegistry);
    private static readonly IReadOnlySet<string> KnownTriggers = new HashSet<string>(StringComparer.Ordinal)
    {
        EffectTriggerIds.Apply,
        EffectTriggerIds.TurnStart,
        EffectTriggerIds.Move,
        EffectTriggerIds.Death,
        EffectTriggerIds.ActorBecameAdjacent,
        EffectTriggerIds.BeforeDamage,
        EffectTriggerIds.AfterSpellResolved,
        RuleTriggerIds.PushCollision,
        RuleTriggerIds.AfterDamage,
        RuleTriggerIds.AfterMove,
        RuleTriggerIds.TileStateChanged,
        RuleTriggerIds.EffectApplied
    };

    public static IEnumerable<RelicDefinition> All => Registry.Value.Definitions.Values;

    public static RelicDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out RelicDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<RelicDefinition> LoadRegistry()
    {
        ContentRegistryResult<RelicDefinition> result = ContentRegistry.Build(
            "relic",
            ContentJsonLoader.LoadItemsWithSources<RelicContentFile, RelicContentDefinition>(
                "relics.json",
                file => file.Relics),
            Resolve);

        return new ContentRegistryResult<RelicDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<RelicDefinition> Resolve(RelicContentDefinition content)
    {
        var issues = new List<string>();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.DescriptionKey, nameof(content.DescriptionKey), issues);

        foreach (RelicHookContentDefinition hook in content.Hooks)
        {
            if (!KnownTriggers.Contains(hook.Trigger))
            {
                issues.Add($"hook trigger '{hook.Trigger}' is invalid.");
            }

            ValidateBehaviorReference(hook.BehaviorId, issues);
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<RelicDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new RelicDefinition(
            content.Id,
            content.DisplayNameKey,
            content.DescriptionKey,
            content.Hooks
                .Select(hook => new RelicHookDefinition(hook.Trigger, hook.BehaviorId, hook.Priority))
                .ToArray(),
            content.Tags));
    }

    private static void RequireStringKey(string key, string field, List<string> issues)
    {
        if (!ContentRegistry.HasString(key))
        {
            issues.Add($"{field} references missing string key '{key}'.");
        }
    }

    private static void ValidateBehaviorReference(string behaviorId, List<string> issues)
    {
        if (BehaviorDefinitionCatalog.IsDisabled(behaviorId))
        {
            issues.Add($"behaviorId references disabled behavior '{behaviorId}'.");
        }
        else if (!BehaviorDefinitionCatalog.TryGet(behaviorId, out _))
        {
            issues.Add($"behaviorId references missing behavior '{behaviorId}'.");
        }
    }

    private sealed class RelicContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<RelicContentDefinition> Relics { get; set; } = [];
    }

    private sealed class RelicContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public List<RelicHookContentDefinition> Hooks { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    private sealed class RelicHookContentDefinition
    {
        public string Trigger { get; set; } = "";
        public string BehaviorId { get; set; } = "";
        public int Priority { get; set; }
    }
}
