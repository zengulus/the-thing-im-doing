using System.Collections.Generic;
using TheThingImDoing.Content;

namespace TheThingImDoing.Relics;

public sealed record RelicDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    IReadOnlyList<RelicHookDefinition> Hooks,
    IReadOnlyList<string> Tags)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Description => GameStrings.Get(DescriptionKey);
}

public sealed record RelicHookDefinition(string Trigger, string BehaviorId);
