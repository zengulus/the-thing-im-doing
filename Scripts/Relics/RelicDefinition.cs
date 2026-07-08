using System.Collections.Generic;
using System.Linq;
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

    public IEnumerable<string> GetBehaviorIds(string trigger)
    {
        foreach (RelicHookDefinition hook in Hooks
                     .Where(hook => hook.Trigger == trigger && !string.IsNullOrWhiteSpace(hook.BehaviorId))
                     .OrderBy(hook => hook.Priority))
        {
            yield return hook.BehaviorId;
        }
    }
}

public sealed record RelicHookDefinition(string Trigger, string BehaviorId, int Priority = 0);
