using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Behaviors;

public static class BehaviorPrimitiveCatalog
{
    private static readonly Lazy<ContentRegistryResult<BehaviorPrimitiveDefinition>> Registry = new(LoadRegistry);

    public static IEnumerable<BehaviorPrimitiveDefinition> All => Registry.Value.Definitions.Values;

    public static BehaviorPrimitiveDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out BehaviorPrimitiveDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    internal static void ValidateBehaviorReferences(IReadOnlySet<string> behaviorIds, IReadOnlySet<string> disabledBehaviorIds)
    {
        foreach (BehaviorPrimitiveDefinition primitive in All)
        {
            if (string.IsNullOrWhiteSpace(primitive.BehaviorId))
            {
                continue;
            }

            if (disabledBehaviorIds.Contains(primitive.BehaviorId))
            {
                ContentDiagnostics.Warn(
                    $"behavior primitive '{primitive.Id}' references disabled behavior '{primitive.BehaviorId}'.");
            }
            else if (!behaviorIds.Contains(primitive.BehaviorId))
            {
                ContentDiagnostics.Warn(
                    $"behavior primitive '{primitive.Id}' references missing behavior '{primitive.BehaviorId}'.");
            }
        }
    }

    private static ContentRegistryResult<BehaviorPrimitiveDefinition> LoadRegistry()
    {
        ContentRegistryResult<BehaviorPrimitiveDefinition> result = ContentRegistry.Build(
            "behavior primitive",
            ContentJsonLoader.LoadItemsWithSources<BehaviorPrimitiveContentFile, BehaviorPrimitiveContentDefinition>(
                "behavior_primitives.json",
                file => file.Primitives),
            Resolve);

        return new ContentRegistryResult<BehaviorPrimitiveDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<BehaviorPrimitiveDefinition> Resolve(BehaviorPrimitiveContentDefinition content)
    {
        var issues = new List<string>();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.DescriptionKey, nameof(content.DescriptionKey), issues);

        foreach (BehaviorPrimitiveParameterContentDefinition parameter in content.Parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                issues.Add("parameter has a blank name.");
            }

            if (string.IsNullOrWhiteSpace(parameter.Type))
            {
                issues.Add($"parameter '{parameter.Name}' has a blank type.");
            }

            if (parameter.Type == "int"
                && !string.IsNullOrWhiteSpace(parameter.Default)
                && !int.TryParse(parameter.Default, out _))
            {
                issues.Add($"parameter '{parameter.Name}' has invalid int default '{parameter.Default}'.");
            }
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<BehaviorPrimitiveDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new BehaviorPrimitiveDefinition(
            content.Id,
            content.DisplayNameKey,
            content.DescriptionKey,
            string.IsNullOrWhiteSpace(content.BehaviorId) ? null : content.BehaviorId,
            content.Scopes,
            content.Tags,
            content.Parameters
                .Select(parameter => new BehaviorPrimitiveParameterDefinition(
                    parameter.Name,
                    parameter.Type,
                    parameter.Required,
                    parameter.Default,
                    parameter.AllowedValues))
                .ToArray()));
    }

    private static void RequireStringKey(string key, string field, List<string> issues)
    {
        if (!ContentRegistry.HasString(key))
        {
            issues.Add($"{field} references missing string key '{key}'.");
        }
    }

    private sealed class BehaviorPrimitiveContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<BehaviorPrimitiveContentDefinition> Primitives { get; set; } = [];
    }

    private sealed class BehaviorPrimitiveContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public string? BehaviorId { get; set; }
        public List<string> Scopes { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<BehaviorPrimitiveParameterContentDefinition> Parameters { get; set; } = [];
    }

    private sealed class BehaviorPrimitiveParameterContentDefinition
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public bool Required { get; set; }
        public string Default { get; set; } = "";
        public List<string> AllowedValues { get; set; } = [];
    }
}
