using System.Linq;
using TheThingImDoing.Progression;
using TheThingImDoing.Core;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class RunPlayerStateTests
{
    [Fact]
    public void RewardCatalog_LoadsEveryAtomicRewardKind()
    {
        Assert.Contains(RewardDefinitionCatalog.All, reward => reward.Kind == RewardKind.Heal);
        Assert.Contains(RewardDefinitionCatalog.All, reward => reward.Kind == RewardKind.MaxHealth);
        Assert.Contains(RewardDefinitionCatalog.All, reward => reward.Kind == RewardKind.UnlockClause);
        Assert.Contains(RewardDefinitionCatalog.All, reward => reward.Kind == RewardKind.Relic);
    }

    [Fact]
    public void ApplyReward_ComposesHealthClauseAndRelicProgression()
    {
        var state = new RunPlayerState(5, new[] { "clause.damage_them" });

        state.ApplyReward(RewardDefinitionCatalog.Get("reward.deeper_vessel"));
        state.ApplyReward(RewardDefinitionCatalog.Get("reward.venom_lexicon"));
        state.ApplyReward(RewardDefinitionCatalog.Get("reward.patient_bell"));

        Assert.Equal(6, state.MaxHealth);
        Assert.Equal(6, state.CurrentHealth);
        Assert.Contains("clause.poison_them", state.UnlockedClauseIds);
        Assert.Contains("clause.spend_poison", state.UnlockedClauseIds);
        Assert.Contains("relic.patient_bell", state.RelicIds);
        Assert.Contains("clause.spend_focus", state.UnlockedClauseIds);
    }

    [Fact]
    public void HealthRewards_ClampIntMaxAmountsWithoutOverflowingFutureEncounterState()
    {
        var state = new RunPlayerState(5, System.Array.Empty<string>());
        var maxHealth = new RewardDefinition(
            "reward.test.max",
            "rewards.deeper_vessel.name",
            "rewards.deeper_vessel.description",
            RewardKind.MaxHealth,
            int.MaxValue,
            "",
            System.Array.Empty<string>());

        Assert.True(state.ApplyReward(maxHealth));
        Assert.Equal(int.MaxValue, state.MaxHealth);
        Assert.Equal(int.MaxValue, state.CurrentHealth);
        Assert.False(state.ApplyReward(maxHealth));

        var encounter = new TacticalEncounter(
            3,
            3,
            new GridPos(1, 1),
            "rule.brittle_stone",
            state.CurrentHealth,
            state.MaxHealth);

        Assert.Equal(int.MaxValue, encounter.Player.Health);
        Assert.Equal(int.MaxValue, encounter.Player.MaxHealth);
    }

    [Fact]
    public void HealReward_IntMaxAmountStopsAtMaxHealthAndCatalogAmountsAreSane()
    {
        var state = new RunPlayerState(5, System.Array.Empty<string>());
        var damagedEncounter = new TacticalEncounter(
            3,
            3,
            new GridPos(1, 1),
            "rule.brittle_stone",
            playerHealth: 1,
            playerMaxHealth: 5);
        state.CaptureEncounterResult(damagedEncounter);
        var heal = new RewardDefinition(
            "reward.test.heal",
            "rewards.mending_draught.name",
            "rewards.mending_draught.description",
            RewardKind.Heal,
            int.MaxValue,
            "",
            System.Array.Empty<string>());

        Assert.True(state.ApplyReward(heal));
        Assert.Equal(5, state.CurrentHealth);
        Assert.All(
            RewardDefinitionCatalog.All.Where(reward =>
                reward.Kind is RewardKind.Heal or RewardKind.MaxHealth),
            reward => Assert.InRange(
                reward.Amount,
                1,
                RewardDefinitionCatalog.MaximumHealthRewardAmount));
    }
}
