using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;

namespace TheThingImDoing.World;

public sealed record FloorRuleDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    IReadOnlyList<RuleHookDefinition> Hooks,
    IReadOnlyList<string> Tags)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Description => GameStrings.Get(DescriptionKey);

    public IEnumerable<string> GetBehaviorIds(string trigger)
    {
        foreach (RuleHookDefinition hook in Hooks
                     .Where(hook => hook.Trigger == trigger && !string.IsNullOrWhiteSpace(hook.BehaviorId))
                     .OrderBy(hook => hook.Priority))
        {
            yield return hook.BehaviorId;
        }
    }
}

public sealed record RuleHookDefinition(string Trigger, string BehaviorId, int Priority = 0);

public static class RuleTriggerIds
{
    public const string PushCollision = "push_collision";
    public const string BeforeDamage = "before_damage";
    public const string AfterDamage = "after_damage";
    public const string AfterMove = "after_move";
    public const string TileStateChanged = "tile_state_changed";
    public const string EffectApplied = "effect_applied";
    public const string AfterSpellResolved = "after_spell_resolved";
}

public static class FloorRuleDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<FloorRuleDefinition>> Registry = new(LoadRegistry);
    private static readonly IReadOnlySet<string> KnownTriggers = new HashSet<string>(StringComparer.Ordinal)
    {
        RuleTriggerIds.PushCollision,
        RuleTriggerIds.BeforeDamage,
        RuleTriggerIds.AfterDamage,
        RuleTriggerIds.AfterMove,
        RuleTriggerIds.TileStateChanged,
        RuleTriggerIds.EffectApplied,
        RuleTriggerIds.AfterSpellResolved
    };

    public static IEnumerable<FloorRuleDefinition> All => Registry.Value.Definitions.Values;

    public static FloorRuleDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out FloorRuleDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<FloorRuleDefinition> LoadRegistry()
    {
        ContentRegistryResult<FloorRuleDefinition> result = ContentRegistry.Build(
            "local rule",
            ContentJsonLoader.LoadItemsWithSources<FloorRuleContentFile, FloorRuleContentDefinition>(
                "rules.json",
                file => file.Rules),
            Resolve);

        return new ContentRegistryResult<FloorRuleDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<FloorRuleDefinition> Resolve(FloorRuleContentDefinition content)
    {
        var issues = new List<string>();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.DescriptionKey, nameof(content.DescriptionKey), issues);

        foreach (RuleHookContentDefinition hook in content.Hooks)
        {
            if (!KnownTriggers.Contains(hook.Trigger))
            {
                issues.Add($"hook trigger '{hook.Trigger}' is invalid.");
            }

            ValidateBehaviorReference(hook.BehaviorId, issues);
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<FloorRuleDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new FloorRuleDefinition(
            content.Id,
            content.DisplayNameKey,
            content.DescriptionKey,
            content.Hooks
                .Select(hook => new RuleHookDefinition(hook.Trigger, hook.BehaviorId, hook.Priority))
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

    private sealed class FloorRuleContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<FloorRuleContentDefinition> Rules { get; set; } = [];
    }

    private sealed class FloorRuleContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public List<RuleHookContentDefinition> Hooks { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    private sealed class RuleHookContentDefinition
    {
        public string Trigger { get; set; } = "";
        public string BehaviorId { get; set; } = "";
        public int Priority { get; set; }
    }
}
