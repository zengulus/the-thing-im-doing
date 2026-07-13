using System.Linq;
using TheThingImDoing.Core;
using TheThingImDoing.UI;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class StatusTextFormatterTests
{
    [Fact]
    public void TenVisibleEnemies_AreCollapsedToBoundedStatusLines()
    {
        string[] intents = Enumerable.Range(1, 10)
            .Select(index => $"Enemy {index}: intent")
            .ToArray();

        string summary = StatusTextFormatter.SummarizeEnemyIntents(intents);

        Assert.Equal(StatusTextFormatter.MaximumEnemyIntentLines + 1, summary.Split('\n').Length);
        Assert.EndsWith("… 6 more threats in sight.", summary);
    }

    [Fact]
    public void ActorDetails_NamePlayerCountersAndEffectStacksWithoutLeakingAiCounters()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        EncounterActor actor = encounter.Player;
        actor.Counters.Add("counter.bonus.focus", 2);
        actor.Counters.Add("counter.bonus.memory", 1);
        actor.Counters.Add("counter.bonus.charge", 3);
        actor.Counters.Add("counter.ai.ritual", 99);
        encounter.AttachEffectToActor(actor.Id, "effect.ward", actor.Id, stacks: 2);
        encounter.AttachEffectToActor(actor.Id, "effect.poison", actor.Id, stacks: 3);
        encounter.AttachEffectToActor(actor.Id, "effect.bleed", actor.Id, stacks: 2);

        string counters = StatusTextFormatter.FormatActorCounters(actor);
        string effects = StatusTextFormatter.FormatActorEffects(actor.Effects);

        Assert.Equal("Focus 2, Memory 1, Charge 3", counters);
        Assert.DoesNotContain("ai", counters, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bleed ×2, Poison ×3, Ward ×2", effects);
    }

    [Fact]
    public void EffectStackSummary_SaturatesMultipleMaxValueOwnersWithoutOverflow()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(4, 1));
        encounter.AttachEffectToActor(
            encounter.Player.Id,
            "effect.poison",
            encounter.Player.Id,
            stacks: int.MaxValue);
        encounter.AttachEffectToActor(
            encounter.Player.Id,
            "effect.poison",
            enemy.Id,
            stacks: int.MaxValue);

        string effects = StatusTextFormatter.FormatActorEffects(encounter.Player.Effects);

        Assert.Equal($"Poison ×{int.MaxValue}", effects);
    }
}
