using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.World;

public sealed record EnvironmentDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    string FloorRuleId,
    string BackgroundColorHex,
    string GridColorHex,
    IReadOnlyList<string> Tags)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Description => GameStrings.Get(DescriptionKey);
}

public static class EnvironmentDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<EnvironmentDefinition>> Registry = new(LoadRegistry);

    public static IEnumerable<EnvironmentDefinition> All => Registry.Value.Definitions.Values;

    public static EnvironmentDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out EnvironmentDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<EnvironmentDefinition> LoadRegistry()
    {
        ContentRegistryResult<EnvironmentDefinition> result = ContentRegistry.Build(
            "environment",
            ContentJsonLoader.LoadItemsWithSources<EnvironmentContentFile, EnvironmentContentDefinition>(
                "environments.json",
                file => file.Environments),
            Resolve);

        return new ContentRegistryResult<EnvironmentDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<EnvironmentDefinition> Resolve(EnvironmentContentDefinition content)
    {
        var issues = new List<string>();
        string floorRuleId = (content.FloorRuleId ?? "").Trim();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.DescriptionKey, nameof(content.DescriptionKey), issues);

        if (FloorRuleDefinitionCatalog.IsDisabled(floorRuleId))
        {
            issues.Add($"floorRuleId references disabled local rule '{floorRuleId}'.");
        }
        else if (!FloorRuleDefinitionCatalog.TryGet(floorRuleId, out _))
        {
            issues.Add($"floorRuleId references missing local rule '{floorRuleId}'.");
        }

        ValidateColor(content.BackgroundColorHex, nameof(content.BackgroundColorHex), issues);
        ValidateColor(content.GridColorHex, nameof(content.GridColorHex), issues);

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<EnvironmentDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new EnvironmentDefinition(
            content.Id.Trim(),
            (content.DisplayNameKey ?? "").Trim(),
            (content.DescriptionKey ?? "").Trim(),
            floorRuleId,
            content.BackgroundColorHex ?? "",
            content.GridColorHex ?? "",
            (content.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()));
    }

    private static void RequireStringKey(string? key, string field, List<string> issues)
    {
        if (!ContentRegistry.HasString(key?.Trim() ?? ""))
        {
            issues.Add($"{field} references missing string key '{key}'.");
        }
    }

    private static void ValidateColor(string? hex, string field, List<string> issues)
    {
        if (hex == null
            || hex.Length != 7
            || hex[0] != '#'
            || !int.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            issues.Add($"{field} color '{hex}' must use #RRGGBB format.");
        }
    }

    private sealed class EnvironmentContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<EnvironmentContentDefinition>? Environments { get; set; } = [];
    }

    private sealed class EnvironmentContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public string? DescriptionKey { get; set; } = "";
        public string? FloorRuleId { get; set; } = "";
        public string? BackgroundColorHex { get; set; } = "";
        public string? GridColorHex { get; set; } = "";
        public List<string>? Tags { get; set; } = [];
    }
}
