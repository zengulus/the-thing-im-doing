using System.Collections.Generic;

namespace TheThingImDoing.Spells;

public sealed record LingeringEffectDefinition(
    string Id,
    string DisplayNameKey,
    string OnApplyBehaviorId,
    string OnTurnStartBehaviorId,
    string OnMoveBehaviorId,
    string OnDeathBehaviorId,
    string OnActorBecameAdjacentBehaviorId,
    string OnBeforeDamageBehaviorId,
    IReadOnlySet<string> Counters)
{
    public string DisplayName => Content.GameStrings.Get(DisplayNameKey);

    public bool AllowsCounter(string counterId)
    {
        return Counters.Contains(counterId);
    }
}
