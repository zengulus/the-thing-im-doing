using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;
using TheThingImDoing.World;

namespace TheThingImDoing.Behaviors;

public static class BehaviorDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<BehaviorDefinition>> Registry = new(LoadRegistry);

    public static BehaviorDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out BehaviorDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<BehaviorDefinition> LoadRegistry()
    {
        ContentRegistryResult<BehaviorDefinition> result = ContentRegistry.Build(
            "behavior",
            ContentJsonLoader.LoadItemsWithSources<BehaviorContentFile, BehaviorContentDefinition>(
                "behaviors.json",
                file => file.Behaviors),
            Resolve);

        BehaviorPrimitiveCatalog.ValidateBehaviorReferences(
            result.Definitions.Keys.ToHashSet(StringComparer.Ordinal),
            result.DisabledIds);

        return new ContentRegistryResult<BehaviorDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<BehaviorDefinition> Resolve(BehaviorContentDefinition content)
    {
        var issues = new List<string>();
        BehaviorStepContentDefinition[] steps = (content.Steps ?? [])
            .OfType<BehaviorStepContentDefinition>()
            .ToArray();

        if (steps.Length == 0)
        {
            issues.Add("has no steps.");
        }

        if ((content.Steps ?? []).Any(step => step == null))
        {
            issues.Add("steps contains a null step.");
        }

        HashSet<int> stepIds = steps.Select(step => step.Id).ToHashSet();

        if (stepIds.Count != steps.Length)
        {
            issues.Add("has duplicate step ids.");
        }

        foreach (BehaviorStepContentDefinition step in steps)
        {
            issues.AddRange(ValidateStep(step, stepIds));
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<BehaviorDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new BehaviorDefinition(
            content.Id?.Trim() ?? "",
            steps
                .Select(step => new BehaviorStepDefinition(
                    step.Id,
                    step.Op ?? "",
                    step.Next,
                    step.True,
                    step.False,
                    step.Amount,
                    step.Maximum,
                    step.Counter ?? "",
                    step.Effect ?? "",
                    step.Relation ?? "",
                    step.Ref ?? "",
                    step.State ?? "",
                    step.Target ?? "",
                    step.Source ?? "",
                    step.Mode ?? ""))
                .ToArray()));
    }

    private static IEnumerable<string> ValidateStep(
        BehaviorStepContentDefinition step,
        IReadOnlySet<int> stepIds)
    {
        if (step.Id <= 0)
        {
            yield return $"step {step.Id} must have a positive id.";
        }

        if (string.IsNullOrWhiteSpace(step.Op))
        {
            yield return $"step {step.Id} has a blank op.";
            yield break;
        }

        foreach ((string label, int? targetId) in new[] { ("next", step.Next), ("true", step.True), ("false", step.False) })
        {
            if (targetId.HasValue && !stepIds.Contains(targetId.Value))
            {
                yield return $"step {step.Id} {label} references missing step {targetId.Value}.";
            }
        }

        if (!BehaviorPrimitiveCatalog.TryGet(step.Op, out BehaviorPrimitiveDefinition? primitive))
        {
            yield return BehaviorPrimitiveCatalog.IsDisabled(step.Op)
                ? $"step {step.Id} references disabled primitive '{step.Op}'."
                : $"step {step.Id} references missing primitive '{step.Op}'.";
            yield break;
        }

        foreach (BehaviorPrimitiveParameterDefinition parameter in primitive.Parameters ?? Array.Empty<BehaviorPrimitiveParameterDefinition>())
        {
            string value = GetParameterValue(step, parameter.Name);

            if (parameter.Required && string.IsNullOrWhiteSpace(value))
            {
                yield return $"step {step.Id} op '{step.Op}' is missing required parameter '{parameter.Name}'.";
            }

            if (parameter.AllowedValues is { Count: > 0 }
                && !string.IsNullOrWhiteSpace(value)
                && !parameter.AllowedValues.Contains(value, StringComparer.Ordinal))
            {
                yield return $"step {step.Id} op '{step.Op}' has invalid {parameter.Name} '{value}'.";
            }

            if (parameter.Type == "tile_state"
                && !string.IsNullOrWhiteSpace(value)
                && (!Enum.TryParse(value, ignoreCase: true, out TileState state)
                    || !Enum.IsDefined(state)
                    || int.TryParse(value, out _)))
            {
                yield return $"step {step.Id} op '{step.Op}' has invalid tile state '{value}'.";
            }
        }

        if (step.Maximum.HasValue && step.Maximum.Value <= 0)
        {
            yield return $"step {step.Id} op '{step.Op}' maximum must be positive.";
        }
    }

    private static string GetParameterValue(BehaviorStepContentDefinition step, string name)
    {
        return name switch
        {
            "amount" => step.Amount?.ToString() ?? "",
            "maximum" => step.Maximum?.ToString() ?? "",
            "counter" => step.Counter ?? "",
            "effect" => step.Effect ?? "",
            "relation" => step.Relation ?? "",
            "ref" => step.Ref ?? "",
            "state" => step.State ?? "",
            "target" => step.Target ?? "",
            "source" => step.Source ?? "",
            "mode" => step.Mode ?? "",
            _ => ""
        };
    }

    private sealed class BehaviorContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<BehaviorContentDefinition>? Behaviors { get; set; } = [];
    }

    private sealed class BehaviorContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public List<BehaviorStepContentDefinition?>? Steps { get; set; } = [];
    }

    private sealed class BehaviorStepContentDefinition
    {
        public int Id { get; set; }
        public string? Op { get; set; } = "";
        public int? Next { get; set; }
        public int? True { get; set; }
        public int? False { get; set; }
        public int? Amount { get; set; }
        public int? Maximum { get; set; }
        public string? Counter { get; set; } = "";
        public string? Effect { get; set; } = "";
        public string? Relation { get; set; } = "";
        public string? Ref { get; set; } = "";
        public string? State { get; set; } = "";
        public string? Target { get; set; } = "";
        public string? Source { get; set; } = "";
        public string? Mode { get; set; } = "";
    }
}
