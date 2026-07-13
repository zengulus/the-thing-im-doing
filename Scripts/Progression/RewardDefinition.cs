using System.Collections.Generic;
using TheThingImDoing.Content;

namespace TheThingImDoing.Progression;

public enum RewardKind
{
    Heal,
    MaxHealth,
    UnlockClause,
    Relic
}

public sealed record RewardDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    RewardKind Kind,
    int Amount,
    string ContentId,
    IReadOnlyList<string> Tags)
{
    public IReadOnlyList<string> GrantedClauseIds { get; init; } = Kind == RewardKind.UnlockClause
        ? [ContentId]
        : [];

    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Description => GameStrings.Get(DescriptionKey);
}
