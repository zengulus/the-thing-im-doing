using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Core;

namespace TheThingImDoing.Progression;

public sealed class RunPlayerState
{
    private readonly HashSet<string> _unlockedClauseIds;
    private readonly HashSet<string> _relicIds;

    public RunPlayerState(
        int maxHealth,
        IEnumerable<string> unlockedClauseIds,
        IEnumerable<string>? relicIds = null)
    {
        MaxHealth = Math.Max(1, maxHealth);
        CurrentHealth = MaxHealth;
        _unlockedClauseIds = unlockedClauseIds.ToHashSet(StringComparer.Ordinal);
        _relicIds = (relicIds ?? []).ToHashSet(StringComparer.Ordinal);
    }

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public IReadOnlySet<string> UnlockedClauseIds => _unlockedClauseIds;
    public IReadOnlySet<string> RelicIds => _relicIds;

    public void CaptureEncounterResult(TacticalEncounter encounter)
    {
        CurrentHealth = Math.Clamp(encounter.Player.Health, 0, MaxHealth);
    }

    public bool ApplyReward(RewardDefinition reward)
    {
        bool changed;

        switch (reward.Kind)
        {
            case RewardKind.Heal:
                int healedHealth = (int)Math.Min(
                    MaxHealth,
                    (long)CurrentHealth + Math.Max(0L, reward.Amount));
                changed = healedHealth != CurrentHealth;
                CurrentHealth = healedHealth;
                break;

            case RewardKind.MaxHealth:
                long positiveAmount = Math.Max(0L, reward.Amount);
                int nextMaxHealth = (int)Math.Min(int.MaxValue, (long)MaxHealth + positiveAmount);
                int nextCurrentHealth = (int)Math.Min(
                    nextMaxHealth,
                    (long)CurrentHealth + positiveAmount);
                changed = nextMaxHealth != MaxHealth || nextCurrentHealth != CurrentHealth;
                MaxHealth = nextMaxHealth;
                CurrentHealth = nextCurrentHealth;
                break;

            case RewardKind.UnlockClause:
                changed = false;
                break;

            case RewardKind.Relic:
                changed = _relicIds.Add(reward.ContentId);
                break;

            default:
                return false;
        }

        foreach (string clauseId in reward.GrantedClauseIds)
        {
            changed |= _unlockedClauseIds.Add(clauseId);
        }

        return changed;
    }
}
