using Godot;
using TheThingImDoing.Content;
using TheThingImDoing.Spells;

namespace TheThingImDoing.UI;

public static class ClauseRolePresentation
{
    public static string GetName(ClauseRole role)
    {
        return GameStrings.Get(role switch
        {
            ClauseRole.Generator => "clauses.role.generator.name",
            ClauseRole.Operator => "clauses.role.operator.name",
            ClauseRole.Consumer => "clauses.role.consumer.name",
            _ => "clauses.role.operator.name"
        });
    }

    public static string GetShortName(ClauseRole role)
    {
        return GameStrings.Get(role switch
        {
            ClauseRole.Generator => "clauses.role.generator.short",
            ClauseRole.Operator => "clauses.role.operator.short",
            ClauseRole.Consumer => "clauses.role.consumer.short",
            _ => "clauses.role.operator.short"
        });
    }

    public static Color GetColor(ClauseRole role)
    {
        return role switch
        {
            ClauseRole.Generator => new Color(0.38f, 0.82f, 0.96f),
            ClauseRole.Operator => new Color(0.82f, 0.65f, 1.0f),
            ClauseRole.Consumer => new Color(1.0f, 0.66f, 0.30f),
            _ => GameTheme.MutedText
        };
    }
}
