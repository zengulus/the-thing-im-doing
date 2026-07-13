using System.Collections.Generic;
using TheThingImDoing.Core;
using TheThingImDoing.Progression;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class RewardOfferPolicyTests
{
    [Fact]
    public void FirstVictory_AlwaysOffersVenomLexiconFirst()
    {
        var playerState = new RunPlayerState(5, ["clause.damage_them"]);

        var choices = RewardOfferPolicy.BuildOffer(
            RewardDefinitionCatalog.All,
            playerState,
            completedVictories: 1,
            maximumUnlockChoices: 2);

        Assert.Equal(3, choices.Count);
        Assert.Equal(RewardOfferPolicy.FirstVictoryUnlockId, choices[0].Id);
        Assert.Contains(choices, reward => reward.Id == RewardOfferPolicy.DeeperVesselRewardId);
        Assert.DoesNotContain(choices, reward => reward.Id == RewardOfferPolicy.PatientBellRewardId);
    }

    [Fact]
    public void VenomLexicon_RemainsOfferedAfterChoosingFirstVictoryRecovery()
    {
        var playerState = new RunPlayerState(5, ["clause.damage_them"]);

        IReadOnlyList<RewardDefinition> choices = RewardOfferPolicy.BuildOffer(
            RewardDefinitionCatalog.All,
            playerState,
            completedVictories: 2,
            maximumUnlockChoices: 2);

        Assert.Contains(choices, reward => reward.Id == RewardOfferPolicy.FirstVictoryUnlockId);
        Assert.Contains(choices, reward => reward.Id == RewardOfferPolicy.PatientBellRewardId);
    }

    [Theory]
    [InlineData(3, "reward.mending_draught")]
    [InlineData(5, "reward.deeper_vessel")]
    public void LaterVictory_ReservesBellAndContextualSurvivalSlot(
        int currentHealth,
        string expectedSurvivalRewardId)
    {
        var playerState = new RunPlayerState(5, ["clause.damage_them"]);
        var encounter = new TacticalEncounter(
            3,
            3,
            new GridPos(1, 1),
            "rule.brittle_stone",
            currentHealth,
            playerMaxHealth: 5);
        playerState.CaptureEncounterResult(encounter);

        var choices = RewardOfferPolicy.BuildOffer(
            RewardDefinitionCatalog.All,
            playerState,
            completedVictories: 2,
            maximumUnlockChoices: 2);

        Assert.Equal(3, choices.Count);
        Assert.Single(choices, reward => reward.Kind == RewardKind.UnlockClause);
        Assert.Contains(choices, reward => reward.Id == RewardOfferPolicy.PatientBellRewardId);
        Assert.Contains(choices, reward => reward.Id == expectedSurvivalRewardId);
    }
}
