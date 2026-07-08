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
                        step.State))
                    .ToArray());
        }

        return definitions;
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
    }
}
