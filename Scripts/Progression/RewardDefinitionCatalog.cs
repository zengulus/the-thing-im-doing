using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;
using TheThingImDoing.Relics;
using TheThingImDoing.Spells;

namespace TheThingImDoing.Progression;

public static class RewardDefinitionCatalog
{
    public const int MaximumHealthRewardAmount = 100;

    private static readonly Lazy<ContentRegistryResult<RewardDefinition>> Registry = new(LoadRegistry);

    public static IEnumerable<RewardDefinition> All => Registry.Value.Definitions.Values;

    public static RewardDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out RewardDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    private static ContentRegistryResult<RewardDefinition> LoadRegistry()
    {
        ContentRegistryResult<RewardDefinition> result = ContentRegistry.Build(
            "reward",
            ContentJsonLoader.LoadItemsWithSources<RewardContentFile, RewardContentDefinition>(
                "rewards.json",
                file => file.Rewards),
            Resolve);

        return new ContentRegistryResult<RewardDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<RewardDefinition> Resolve(RewardContentDefinition content)
    {
        var issues = new List<string>();
        string contentId = (content.ContentId ?? "").Trim();
        string[] grantedClauseIds = (content.ClauseIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.DescriptionKey, nameof(content.DescriptionKey), issues);

        if (!Enum.TryParse(content.Kind, ignoreCase: true, out RewardKind kind)
            || !Enum.IsDefined(kind)
            || int.TryParse(content.Kind, out _))
        {
            issues.Add($"kind '{content.Kind}' is invalid.");
        }
        else
        {
            if (kind == RewardKind.UnlockClause && !string.IsNullOrWhiteSpace(contentId))
            {
                grantedClauseIds = grantedClauseIds
                    .Prepend(contentId)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }

            switch (kind)
            {
                case RewardKind.Heal or RewardKind.MaxHealth when content.Amount <= 0:
                    issues.Add("amount must be positive.");
                    break;
                case RewardKind.Heal or RewardKind.MaxHealth when content.Amount > MaximumHealthRewardAmount:
                    issues.Add($"amount cannot exceed {MaximumHealthRewardAmount}.");
                    break;
                case RewardKind.UnlockClause when grantedClauseIds.Length == 0:
                    issues.Add("at least one clause id is required.");
                    break;
                case RewardKind.Relic when !RelicDefinitionCatalog.TryGet(contentId, out _):
                    issues.Add($"contentId references missing relic '{contentId}'.");
                    break;
            }
        }


        foreach (string clauseId in grantedClauseIds)
        {
            if (!ClauseDefinitionCatalog.TryGet(clauseId, out _))
            {
                issues.Add($"clauseIds references missing clause '{clauseId}'.");
            }
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<RewardDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new RewardDefinition(
            content.Id?.Trim() ?? "",
            content.DisplayNameKey ?? "",
            content.DescriptionKey ?? "",
            kind,
            content.Amount,
            contentId,
            (content.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag!).ToArray())
        {
            GrantedClauseIds = grantedClauseIds
        });
    }

    private static void RequireStringKey(string? key, string field, List<string> issues)
    {
        if (!ContentRegistry.HasString(key ?? ""))
        {
            issues.Add($"{field} references missing string key '{key}'.");
        }
    }

    private sealed class RewardContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<RewardContentDefinition> Rewards { get; set; } = [];
    }

    private sealed class RewardContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public string? DescriptionKey { get; set; } = "";
        public string? Kind { get; set; } = "";
        public int Amount { get; set; }
        public string? ContentId { get; set; } = "";
        public List<string?>? ClauseIds { get; set; } = [];
        public List<string?>? Tags { get; set; } = [];
    }
}
