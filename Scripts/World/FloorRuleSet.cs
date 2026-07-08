using System.Collections.Generic;

namespace TheThingImDoing.World;

public sealed class FloorRuleSet
{
    public FloorRuleSet(string activeRuleId)
    {
        ActiveRuleId = activeRuleId;
    }

    public string ActiveRuleId { get; }

    public FloorRuleDefinition? ActiveRule
    {
        get
        {
            return FloorRuleDefinitionCatalog.TryGet(ActiveRuleId, out FloorRuleDefinition? definition)
                ? definition
                : null;
        }
    }

    public string DisplayName => ActiveRule?.DisplayName ?? ActiveRuleId;
    public string Description => ActiveRule?.Description ?? "";

    public IEnumerable<string> GetBehaviorIds(string trigger)
    {
        return ActiveRule?.GetBehaviorIds(trigger) ?? [];
    }

    public static FloorRuleSet BrittleStone()
    {
        return new FloorRuleSet("rule.brittle_stone");
    }
}
