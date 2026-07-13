using System;
using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Content;

public sealed class ContentRegistryResult<TDefinition>
{
    public ContentRegistryResult(
        IReadOnlyDictionary<string, TDefinition> definitions,
        IReadOnlySet<string> disabledIds)
    {
        Definitions = definitions;
        DisabledIds = disabledIds;
    }

    public IReadOnlyDictionary<string, TDefinition> Definitions { get; }
    public IReadOnlySet<string> DisabledIds { get; }
}

public static class ContentRegistry
{
    public static ContentRegistryResult<TDefinition> Build<TContent, TDefinition>(
        string family,
        IEnumerable<LoadedContentItem<TContent>> items,
        Func<TContent, ContentValidationResult<TDefinition>> resolve)
        where TContent : class, IContentDefinition
    {
        var definitions = new Dictionary<string, TDefinition>(StringComparer.Ordinal);
        var disabledIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (LoadedContentItem<TContent> item in items)
        {
            if (item.Value == null)
            {
                ContentDiagnostics.Warn($"{family} content in {item.SourcePath} is null and was skipped.");
                continue;
            }

            string id = (item.Value.Id ?? "").Trim();
            string operation = NormalizeOperation(item.Value.Operation);

            if (string.IsNullOrWhiteSpace(id))
            {
                ContentDiagnostics.Warn($"{family} content in {item.SourcePath} has a blank id and was disabled.");
                continue;
            }

            if (operation is "disable" or "remove")
            {
                definitions.Remove(id);
                disabledIds.Add(id);
                continue;
            }

            if (operation is not ("add" or "replace" or "override"))
            {
                ContentDiagnostics.Warn($"{family} '{id}' in {item.SourcePath} has invalid operation '{operation}'.");
                continue;
            }

            bool replacing = operation is "replace" or "override";

            if (!replacing && disabledIds.Contains(id))
            {
                ContentDiagnostics.Warn(
                    $"{family} '{id}' in {item.SourcePath} tried to add disabled content; use replace to re-enable it.");
                continue;
            }

            if (!replacing && definitions.ContainsKey(id))
            {
                ContentDiagnostics.Warn(
                    $"{family} '{id}' in {item.SourcePath} duplicates an existing id; use replace/override explicitly.");
                continue;
            }

            ContentValidationResult<TDefinition> result;

            try
            {
                result = resolve(item.Value);
            }
            catch (Exception exception)
            {
                ContentDiagnostics.Warn(
                    $"{family} '{id}' in {item.SourcePath} could not be resolved and was skipped: " +
                    $"{exception.GetType().Name}: {exception.Message}");
                continue;
            }

            if (!result.IsValid || result.Value == null)
            {
                foreach (string issue in result.Issues)
                {
                    ContentDiagnostics.Warn($"{family} '{id}' in {item.SourcePath}: {issue}");
                }

                continue;
            }

            disabledIds.Remove(id);
            definitions[id] = result.Value;
        }

        return new ContentRegistryResult<TDefinition>(
            definitions,
            disabledIds);
    }

    public static bool HasString(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && GameStrings.Has(key);
    }

    public static ContentValidationResult<TDefinition> Invalid<TDefinition>(params string[] issues)
    {
        return new ContentValidationResult<TDefinition>(default, issues);
    }

    public static ContentValidationResult<TDefinition> Valid<TDefinition>(TDefinition definition)
    {
        return new ContentValidationResult<TDefinition>(definition, []);
    }

    public static bool HasAnyIssue(IEnumerable<string> issues, out IReadOnlyList<string> issueList)
    {
        issueList = issues.Where(issue => !string.IsNullOrWhiteSpace(issue)).ToArray();
        return issueList.Count > 0;
    }

    private static string NormalizeOperation(string operation)
    {
        return string.IsNullOrWhiteSpace(operation) ? "add" : operation.Trim().ToLowerInvariant();
    }
}

public sealed class ContentValidationResult<TDefinition>
{
    public ContentValidationResult(TDefinition? value, IReadOnlyList<string> issues)
    {
        Value = value;
        Issues = issues;
    }

    public TDefinition? Value { get; }
    public IReadOnlyList<string> Issues { get; }
    public bool IsValid => Issues.Count == 0;
}
