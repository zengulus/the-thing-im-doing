using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public static class EffectDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<EffectDefinition>> Registry = new(LoadRegistry);
    private static readonly IReadOnlySet<string> KnownTriggers = new HashSet<string>(StringComparer.Ordinal)
    {
        EffectTriggerIds.Apply,
        EffectTriggerIds.TurnStart,
        EffectTriggerIds.Move,
        EffectTriggerIds.Death,
        EffectTriggerIds.ActorBecameAdjacent,
        EffectTriggerIds.BeforeDamage,
        EffectTriggerIds.AfterSpellResolved
    };

    public static EffectDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out EffectDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    public static bool IsKnownTrigger(string triggerId)
    {
        return KnownTriggers.Contains(triggerId);
    }

    private static ContentRegistryResult<EffectDefinition> LoadRegistry()
    {
        ContentRegistryResult<EffectDefinition> result = ContentRegistry.Build(
            "effect",
            ContentJsonLoader.LoadItemsWithSources<EffectContentFile, EffectContentDefinition>(
                "effects.json",
                file => file.Effects),
            Resolve);

        return new ContentRegistryResult<EffectDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<EffectDefinition> Resolve(EffectContentDefinition content)
    {
        var issues = new List<string>();
        EffectTriggerContentDefinition[] triggers = (content.Triggers ?? [])
            .OfType<EffectTriggerContentDefinition>()
            .ToArray();
        string[] counters = (content.Counters ?? [])
            .Where(counter => counter != null)
            .Select(counter => counter!)
            .ToArray();

        if (!ContentRegistry.HasString(content.DisplayNameKey ?? ""))
        {
            issues.Add($"displayNameKey references missing string key '{content.DisplayNameKey}'.");
        }

        if ((content.Triggers ?? []).Any(trigger => trigger == null))
        {
            issues.Add("triggers contains a null trigger.");
        }

        foreach (EffectTriggerContentDefinition trigger in triggers)
        {
            string triggerId = trigger.TriggerId ?? "";

            if (!KnownTriggers.Contains(triggerId))
            {
                issues.Add($"trigger '{triggerId}' is invalid.");
            }

            ValidateBehaviorReference(trigger.BehaviorId, issues);
        }

        if ((content.Counters ?? []).Any(counter => counter == null))
        {
            issues.Add("counters contains a null counter id.");
        }

        foreach (string counterId in counters)
        {
            if (string.IsNullOrWhiteSpace(counterId))
            {
                issues.Add("counters contains a blank counter id.");
            }
        }

        if (content.MaxStacks.HasValue && content.MaxStacks.Value <= 0)
        {
            issues.Add("maxStacks must be positive when provided.");
        }

        if (content.MaxStacks.HasValue
            && !counters.Contains("counter.stack", StringComparer.Ordinal))
        {
            issues.Add("maxStacks requires the counter.stack counter.");
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<EffectDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new EffectDefinition(
            content.Id?.Trim() ?? "",
            content.DisplayNameKey ?? "",
            triggers
                .Select(trigger => new EffectTriggerDefinition(
                    trigger.TriggerId ?? "",
                    trigger.BehaviorId ?? "",
                    trigger.Priority))
                .ToArray(),
            counters.ToHashSet(StringComparer.Ordinal),
            content.MaxStacks));
    }

    private static void ValidateBehaviorReference(string? behaviorId, List<string> issues)
    {
        string id = behaviorId ?? "";

        if (BehaviorDefinitionCatalog.IsDisabled(id))
        {
            issues.Add($"behaviorId references disabled behavior '{behaviorId}'.");
        }
        else if (!BehaviorDefinitionCatalog.TryGet(id, out _))
        {
            issues.Add($"behaviorId references missing behavior '{behaviorId}'.");
        }
    }

    private sealed class EffectContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<EffectContentDefinition> Effects { get; set; } = [];
    }

    private sealed class EffectContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public List<EffectTriggerContentDefinition?>? Triggers { get; set; } = [];
        public List<string?>? Counters { get; set; } = [];
        public int? MaxStacks { get; set; }
    }

    private sealed class EffectTriggerContentDefinition
    {
        public string? TriggerId { get; set; } = "";
        public string? BehaviorId { get; set; } = "";
        public int Priority { get; set; }
    }
}
