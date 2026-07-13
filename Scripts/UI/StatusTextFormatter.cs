using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Content;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;

namespace TheThingImDoing.UI;

public static class StatusTextFormatter
{
    public const int MaximumEnemyIntentLines = 4;

    private static readonly (string CounterId, string DisplayNameKey)[] PlayerFacingCounters =
    [
        ("counter.bonus.focus", "counters.focus.name"),
        ("counter.bonus.memory", "counters.memory.name"),
        ("counter.bonus.charge", "counters.charge.name")
    ];

    public static string SummarizeEnemyIntents(IEnumerable<string> enemyIntents)
    {
        string[] intents = enemyIntents.ToArray();
        string summary = string.Join("\n", intents.Take(MaximumEnemyIntentLines));

        if (intents.Length > MaximumEnemyIntentLines)
        {
            summary += $"\n… {intents.Length - MaximumEnemyIntentLines} more threats in sight.";
        }

        return summary;
    }

    public static string FormatActorCounters(EncounterActor actor)
    {
        return string.Join(
            ", ",
            PlayerFacingCounters
                .Select(counter => (
                    Name: GameStrings.Get(counter.DisplayNameKey),
                    Value: actor.Counters.Get(counter.CounterId)))
                .Where(counter => counter.Value > 0)
                .Select(counter => $"{counter.Name} {counter.Value}"));
    }

    public static string FormatActorEffects(IEnumerable<EffectInstance> effects)
    {
        return string.Join(
            ", ",
            effects
                .GroupBy(effect => effect.EffectId)
                .Select(group =>
                {
                    string displayName = EffectDefinitionCatalog.TryGet(
                        group.Key,
                        out EffectDefinition? definition)
                            ? definition.DisplayName
                            : group.Key;
                    int stacks = group.Aggregate(
                        0,
                        (total, effect) => (int)Math.Min(
                            int.MaxValue,
                            (long)total + effect.Counters.Get("counter.stack")));
                    return stacks > 0 ? $"{displayName} ×{stacks}" : displayName;
                })
                .OrderBy(text => text));
    }
}
