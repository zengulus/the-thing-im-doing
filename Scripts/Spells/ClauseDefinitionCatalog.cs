using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public static class ClauseDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<ClauseDefinition>> Registry = new(LoadRegistry);

    public static IEnumerable<ClauseDefinition> All => Registry.Value.Definitions.Values;

    public static ClauseDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out ClauseDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<ClauseDefinition> LoadRegistry()
    {
        ContentRegistryResult<ClauseDefinition> result = ContentRegistry.Build(
            "clause",
            ContentJsonLoader.LoadItemsWithSources<ClauseContentFile, ClauseContentDefinition>(
                "clauses.json",
                file => file.Clauses),
            Resolve);

        return new ContentRegistryResult<ClauseDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.Family)
                .ThenBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<ClauseDefinition> Resolve(ClauseContentDefinition content)
    {
        var issues = new List<string>();
        Dictionary<string, int> counterCosts = content.CounterCosts ?? [];
        Dictionary<string, int> counterGains = content.CounterGains ?? [];

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.PlayerTextKey, nameof(content.PlayerTextKey), issues);

        if (!string.IsNullOrWhiteSpace(content.TooltipKey))
        {
            RequireStringKey(content.TooltipKey, nameof(content.TooltipKey), issues);
        }

        if (!Enum.TryParse(content.Family, ignoreCase: true, out ClauseFamily family)
            || !Enum.IsDefined(family)
            || int.TryParse(content.Family, out _))
        {
            issues.Add($"family '{content.Family}' is invalid.");
        }

        ValidateBehaviorReference(content.BehaviorId, issues);
        ValidateCounters(counterCosts, nameof(content.CounterCosts), issues);
        ValidateCounters(counterGains, nameof(content.CounterGains), issues);

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<ClauseDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new ClauseDefinition(
            content.Id?.Trim() ?? "",
            content.DisplayNameKey ?? "",
            content.PlayerTextKey ?? "",
            family,
            counterCosts,
            counterGains,
            content.TooltipKey ?? "",
            content.BehaviorId ?? "",
            content.IsCondition,
            (content.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag!).ToArray()));
    }

    private static void RequireStringKey(string? key, string field, List<string> issues)
    {
        if (!ContentRegistry.HasString(key ?? ""))
        {
            issues.Add($"{field} references missing string key '{key}'.");
        }
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

    private static void ValidateCounters(Dictionary<string, int> counters, string field, List<string> issues)
    {
        foreach ((string counterId, int amount) in counters)
        {
            if (string.IsNullOrWhiteSpace(counterId))
            {
                issues.Add($"{field} contains a blank counter id.");
            }

            if (amount < 0)
            {
                issues.Add($"{field} contains negative amount {amount} for '{counterId}'.");
            }
        }
    }

    private sealed class ClauseContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<ClauseContentDefinition> Clauses { get; set; } = [];
    }

    private sealed class ClauseContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public string? PlayerTextKey { get; set; } = "";
        public string? TooltipKey { get; set; } = "";
        public string? Family { get; set; } = "";
        public Dictionary<string, int>? CounterCosts { get; set; } = [];
        public Dictionary<string, int>? CounterGains { get; set; } = [];
        public string? BehaviorId { get; set; } = "";
        public bool IsCondition { get; set; }
        public List<string?>? Tags { get; set; } = [];
    }
}
