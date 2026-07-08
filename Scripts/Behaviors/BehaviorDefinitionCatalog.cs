using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Behaviors;

public static class BehaviorDefinitionCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, BehaviorDefinition>> Definitions = new(LoadDefinitions);

    public static BehaviorDefinition Get(string id)
    {
        return Definitions.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out BehaviorDefinition? definition)
    {
        return Definitions.Value.TryGetValue(id, out definition);
    }

    private static IReadOnlyDictionary<string, BehaviorDefinition> LoadDefinitions()
    {
        var definitions = new Dictionary<string, BehaviorDefinition>(StringComparer.Ordinal);

        foreach (BehaviorContentDefinition contentDefinition in ContentJsonLoader.LoadItems<BehaviorContentFile, BehaviorContentDefinition>(
                     "behaviors.json",
                     file => file.Behaviors))
        {
            foreach (BehaviorStepContentDefinition step in contentDefinition.Steps)
            {
                ValidateStep(contentDefinition.Id, step);
            }

            definitions[contentDefinition.Id] = new BehaviorDefinition(
                contentDefinition.Id,
                contentDefinition.Steps
                    .Select(step => new BehaviorStepDefinition(
                        step.Id,
                        step.Op,
                        step.Next,
                        step.True,
                        step.False,
                        step.Amount,
                        step.Counter,
                        step.Effect,
                        step.Relation,
                        step.Ref,
                        step.State,
                        step.Target,
                        step.Source,
                        step.Mode))
                    .ToArray());
        }

        return definitions;
    }

    private static void ValidateStep(string behaviorId, BehaviorStepContentDefinition step)
    {
        if (!BehaviorPrimitiveCatalog.TryGet(step.Op, out BehaviorPrimitiveDefinition? primitive))
        {
            return;
        }

        foreach (BehaviorPrimitiveParameterDefinition parameter in primitive.Parameters ?? [])
        {
            string value = GetParameterValue(step, parameter.Name);

            if (parameter.Required && string.IsNullOrWhiteSpace(value))
            {
                Console.Error.WriteLine(
                    $"Behavior '{behaviorId}' step {step.Id} op '{step.Op}' is missing required parameter '{parameter.Name}'.");
            }

            if (parameter.AllowedValues is { Count: > 0 }
                && !string.IsNullOrWhiteSpace(value)
                && !parameter.AllowedValues.Contains(value, StringComparer.Ordinal))
            {
                Console.Error.WriteLine(
                    $"Behavior '{behaviorId}' step {step.Id} op '{step.Op}' has invalid {parameter.Name} '{value}'.");
            }
        }
    }

    private static string GetParameterValue(BehaviorStepContentDefinition step, string name)
    {
        return name switch
        {
            "amount" => step.Amount?.ToString() ?? "",
            "counter" => step.Counter,
            "effect" => step.Effect,
            "relation" => step.Relation,
            "ref" => step.Ref,
            "state" => step.State,
            "target" => step.Target,
            "source" => step.Source,
            "mode" => step.Mode,
            _ => ""
        };
    }

    private sealed class BehaviorContentFile
    {
        public int SchemaVersion { get; set; }
        public List<BehaviorContentDefinition> Behaviors { get; set; } = [];
    }

    private sealed class BehaviorContentDefinition
    {
        public string Id { get; set; } = "";
        public List<BehaviorStepContentDefinition> Steps { get; set; } = [];
    }

    private sealed class BehaviorStepContentDefinition
    {
        public int Id { get; set; }
        public string Op { get; set; } = "";
        public int? Next { get; set; }
        public int? True { get; set; }
        public int? False { get; set; }
        public int? Amount { get; set; }
        public string Counter { get; set; } = "";
        public string Effect { get; set; } = "";
        public string Relation { get; set; } = "";
        public string Ref { get; set; } = "";
        public string State { get; set; } = "";
        public string Target { get; set; } = "";
        public string Source { get; set; } = "";
        public string Mode { get; set; } = "";
    }
}
