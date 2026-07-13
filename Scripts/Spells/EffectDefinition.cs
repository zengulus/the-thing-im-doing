using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Spells;

public sealed record EffectDefinition(
    string Id,
    string DisplayNameKey,
    IReadOnlyList<EffectTriggerDefinition> Triggers,
    IReadOnlySet<string> Counters,
    int? MaxStacks)
{
    public string DisplayName => Content.GameStrings.Get(DisplayNameKey);

    public bool AllowsCounter(string counterId)
    {
        return Counters.Contains(counterId);
    }

    public IEnumerable<string> GetBehaviorIds(string triggerId)
    {
        foreach (EffectTriggerDefinition trigger in Triggers
                     .Where(trigger => trigger.TriggerId == triggerId && !string.IsNullOrWhiteSpace(trigger.BehaviorId))
                     .OrderBy(trigger => trigger.Priority))
        {
            yield return trigger.BehaviorId;
        }
    }
}

public sealed record EffectTriggerDefinition(string TriggerId, string BehaviorId, int Priority = 0);

public static class EffectTriggerIds
{
    public const string Apply = "apply";
    public const string TurnStart = "turn_start";
    public const string Move = "move";
    public const string Death = "death";
    public const string ActorBecameAdjacent = "actor_became_adjacent";
    public const string BeforeDamage = "before_damage";
    public const string AfterSpellResolved = "after_spell_resolved";
}
