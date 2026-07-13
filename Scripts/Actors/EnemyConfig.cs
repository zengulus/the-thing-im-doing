using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Godot;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;

namespace TheThingImDoing.Actors;

public sealed record EnemyConfig(
    string Id,
    string DisplayNameKey,
    string SigilKey,
    int MaxHealth,
    string PurposeKey,
    string BehaviorId,
    string DefaultIntentKey,
    string TintHex,
    IReadOnlyList<EnemyIntentRule> IntentRules,
    IReadOnlyList<string> Tags)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Sigil => GameStrings.Get(SigilKey);
    public string Purpose => GameStrings.Get(PurposeKey);
    public string DefaultIntent => GameStrings.Get(DefaultIntentKey);
    public Color Tint => ParseColor(TintHex);

    private static Color ParseColor(string hex)
    {
        if (hex.Length == 7
            && hex[0] == '#'
            && int.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int red)
            && int.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int green)
            && int.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int blue))
        {
            return new Color(red / 255.0f, green / 255.0f, blue / 255.0f);
        }

        return new Color(0.90f, 0.35f, 0.32f);
    }
}

public sealed record EnemyIntentRule(string When, string IntentKey, int? Amount = null, string Counter = "");

public static class EnemyConfigCatalog
{
    private static readonly Lazy<ContentRegistryResult<EnemyConfig>> Registry = new(LoadRegistry);
    private static readonly IReadOnlySet<string> KnownIntentRules = new HashSet<string>(StringComparer.Ordinal)
    {
        "adjacent_to_player",
        "adjacent_to_blocking",
        "self_counter_at_least"
    };

    public static IEnumerable<EnemyConfig> All => Registry.Value.Definitions.Values;

    public static EnemyConfig Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out EnemyConfig? config)
    {
        return Registry.Value.Definitions.TryGetValue(id, out config);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<EnemyConfig> LoadRegistry()
    {
        ContentRegistryResult<EnemyConfig> result = ContentRegistry.Build(
            "enemy",
            ContentJsonLoader.LoadItemsWithSources<EnemyContentFile, EnemyContentDefinition>(
                "enemies.json",
                file => file.Enemies),
            Resolve);

        return new ContentRegistryResult<EnemyConfig>(
            result.Definitions
                .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<EnemyConfig> Resolve(EnemyContentDefinition content)
    {
        var issues = new List<string>();
        EnemyIntentContentRule[] intentRules = (content.IntentRules ?? [])
            .OfType<EnemyIntentContentRule>()
            .ToArray();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.SigilKey, nameof(content.SigilKey), issues);
        RequireStringKey(content.PurposeKey, nameof(content.PurposeKey), issues);
        RequireStringKey(content.DefaultIntentKey, nameof(content.DefaultIntentKey), issues);
        ValidateBehaviorReference(content.BehaviorId, issues);

        if (content.MaxHealth <= 0)
        {
            issues.Add("maxHealth must be positive.");
        }

        if (!IsValidHexColor(content.TintHex))
        {
            issues.Add($"tintHex '{content.TintHex}' is invalid.");
        }

        if ((content.IntentRules ?? []).Any(rule => rule == null))
        {
            issues.Add("intentRules contains a null rule.");
        }

        foreach (EnemyIntentContentRule rule in intentRules)
        {
            string when = rule.When ?? "";

            if (!KnownIntentRules.Contains(when))
            {
                issues.Add($"intent rule '{when}' is invalid.");
            }

            RequireStringKey(rule.IntentKey, nameof(rule.IntentKey), issues);

            if (rule.Amount is < 0)
            {
                issues.Add($"intent rule '{when}' has negative amount {rule.Amount}.");
            }
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<EnemyConfig>(null, issueList);
        }

        return ContentRegistry.Valid(new EnemyConfig(
            content.Id?.Trim() ?? "",
            content.DisplayNameKey ?? "",
            content.SigilKey ?? "",
            content.MaxHealth,
            content.PurposeKey ?? "",
            content.BehaviorId ?? "",
            content.DefaultIntentKey ?? "",
            content.TintHex ?? "",
            intentRules
                .Select(rule => new EnemyIntentRule(
                    rule.When ?? "",
                    rule.IntentKey ?? "",
                    rule.Amount,
                    rule.Counter ?? ""))
                .ToArray(),
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

    private static bool IsValidHexColor(string? hex)
    {
        return hex != null
            && hex.Length == 7
            && hex[0] == '#'
            && int.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            && int.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            && int.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }

    private sealed class EnemyContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<EnemyContentDefinition> Enemies { get; set; } = [];
    }

    private sealed class EnemyContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public string? SigilKey { get; set; } = "";
        public int MaxHealth { get; set; }
        public string? PurposeKey { get; set; } = "";
        public string? BehaviorId { get; set; } = "";
        public string? DefaultIntentKey { get; set; } = "";
        public string? TintHex { get; set; } = "#e65a52";
        public List<EnemyIntentContentRule?>? IntentRules { get; set; } = [];
        public List<string?>? Tags { get; set; } = [];
    }

    private sealed class EnemyIntentContentRule
    {
        public string? When { get; set; } = "";
        public string? IntentKey { get; set; } = "";
        public int? Amount { get; set; }
        public string? Counter { get; set; } = "";
    }
}
