using System;
using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Progression;

public static class RewardOfferPolicy
{
    public const string FirstVictoryUnlockId = "reward.venom_lexicon";
    public const string PatientBellRewardId = "reward.patient_bell";
    public const string MendingRewardId = "reward.mending_draught";
    public const string DeeperVesselRewardId = "reward.deeper_vessel";

    public static IReadOnlyList<RewardDefinition> BuildOffer(
        IEnumerable<RewardDefinition> rewards,
        RunPlayerState playerState,
        int completedVictories,
        int maximumUnlockChoices,
        int maximumChoices = 3)
    {
        if (maximumChoices <= 0)
        {
            return [];
        }

        RewardDefinition[] catalog = rewards.ToArray();
        bool reserveBell = completedVictories >= 2
            && !playerState.RelicIds.Contains("relic.patient_bell")
            && catalog.Any(reward => reward.Id == PatientBellRewardId);
        int reservedChoices = 1 + (reserveBell ? 1 : 0);
        int unlockChoiceCount = Math.Min(
            Math.Max(0, maximumUnlockChoices),
            Math.Max(0, maximumChoices - reservedChoices));
        RewardDefinition[] availableUnlocks = catalog
            .Where(reward => reward.Kind == RewardKind.UnlockClause)
            .Where(reward => reward.GrantedClauseIds.Any(
                clauseId => !playerState.UnlockedClauseIds.Contains(clauseId)))
            .ToArray();
        var offer = new List<RewardDefinition>(maximumChoices);

        offer.AddRange(SelectUnlockChoices(
            availableUnlocks,
            completedVictories,
            unlockChoiceCount));

        if (reserveBell)
        {
            AddById(PatientBellRewardId);
        }

        string survivalRewardId = playerState.CurrentHealth <= playerState.MaxHealth - 2
            ? MendingRewardId
            : DeeperVesselRewardId;
        AddById(survivalRewardId);

        return offer.Take(maximumChoices).ToArray();

        void AddById(string rewardId)
        {
            RewardDefinition? reward = catalog.FirstOrDefault(candidate => candidate.Id == rewardId);
            if (reward != null && offer.All(existing => existing.Id != reward.Id))
            {
                offer.Add(reward);
            }
        }
    }

    public static IReadOnlyList<RewardDefinition> SelectUnlockChoices(
        IEnumerable<RewardDefinition> availableRewards,
        int completedVictories,
        int maximumChoices)
    {
        if (maximumChoices <= 0)
        {
            return [];
        }

        RewardDefinition[] ordered = availableRewards
            .Where(reward => reward.Kind == RewardKind.UnlockClause)
            .OrderBy(reward => reward.Id, StringComparer.Ordinal)
            .ToArray();

        if (ordered.Length == 0)
        {
            return [];
        }

        if (completedVictories >= 1
            && ordered.FirstOrDefault(reward => reward.Id == FirstVictoryUnlockId) is RewardDefinition venomUnlock)
        {
            return ordered
                .Prepend(venomUnlock)
                .DistinctBy(reward => reward.Id)
                .Take(maximumChoices)
                .ToArray();
        }

        int startIndex = Math.Max(0, completedVictories - 1) % ordered.Length;
        return Enumerable.Range(0, Math.Min(maximumChoices, ordered.Length))
            .Select(index => ordered[(startIndex + index) % ordered.Length])
            .ToArray();
    }
}
