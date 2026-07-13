using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Core;

public sealed record RunDefinition(
    string Id,
    string DisplayNameKey,
    IReadOnlyList<string> EncounterIds,
    IReadOnlyList<string> Tags)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
}

public static class RunDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<RunDefinition>> Registry = new(LoadRegistry);

    public static IEnumerable<RunDefinition> All => Registry.Value.Definitions.Values;

    public static RunDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out RunDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<RunDefinition> LoadRegistry()
    {
        ContentRegistryResult<RunDefinition> result = ContentRegistry.Build(
            "run",
            ContentJsonLoader.LoadItemsWithSources<RunContentFile, RunContentDefinition>(
                "runs.json",
                file => file.Runs),
            Resolve);

        return new ContentRegistryResult<RunDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<RunDefinition> Resolve(RunContentDefinition content)
    {
        var issues = new List<string>();
        var encounterIds = (content.EncounterIds ?? [])
            .Where(id => id != null)
            .Select(id => id!.Trim())
            .ToArray();

        if (!ContentRegistry.HasString(content.DisplayNameKey?.Trim() ?? ""))
        {
            issues.Add($"displayNameKey references missing string key '{content.DisplayNameKey}'.");
        }

        if (encounterIds.Length == 0)
        {
            issues.Add("at least one encounter id is required.");
        }

        if ((content.EncounterIds ?? []).Any(id => string.IsNullOrWhiteSpace(id)))
        {
            issues.Add("encounterIds contains a blank encounter id.");
        }

        for (int index = 0; index < encounterIds.Length; index++)
        {
            string encounterId = encounterIds[index];

            if (EncounterDefinitionCatalog.IsDisabled(encounterId))
            {
                issues.Add($"encounterIds references disabled encounter '{encounterId}'.");
                continue;
            }

            if (!EncounterDefinitionCatalog.TryGet(encounterId, out EncounterDefinition? encounter))
            {
                issues.Add($"encounterIds references missing encounter '{encounterId}'.");
                continue;
            }

            bool shouldBeFinal = index == encounterIds.Length - 1;

            if (encounter.IsFinal != shouldBeFinal)
            {
                issues.Add(shouldBeFinal
                    ? $"last encounter '{encounterId}' must be marked final."
                    : $"encounter '{encounterId}' is marked final before the end of the run.");
            }
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<RunDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new RunDefinition(
            content.Id.Trim(),
            (content.DisplayNameKey ?? "").Trim(),
            encounterIds,
            (content.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()));
    }

    private sealed class RunContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<RunContentDefinition>? Runs { get; set; } = [];
    }

    private sealed class RunContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public List<string?>? EncounterIds { get; set; } = [];
        public List<string>? Tags { get; set; } = [];
    }
}
