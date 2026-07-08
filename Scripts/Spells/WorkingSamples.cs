using Godot;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public static class WorkingSamples
{
    public static Working CreateMarkOrSpark()
    {
        var working = new Working(GameStrings.Get("workings.mark_or_spark.name"));
        var aim = new WorkingNode(1, "clause.aim_at_nearest_foe", new Vector2(16, 24));
        var condition = new WorkingNode(2, "clause.if_marked", new Vector2(220, 24));
        var spark = new WorkingNode(3, "clause.spark_them", new Vector2(430, 4));
        var mark = new WorkingNode(4, "clause.mark_them", new Vector2(430, 140));

        aim.NextNodeId = condition.Id;
        condition.TrueNodeId = spark.Id;
        condition.FalseNodeId = mark.Id;

        working.AddNode(aim);
        working.AddNode(condition);
        working.AddNode(spark);
        working.AddNode(mark);
        working.EntryNodeId = aim.Id;

        return working;
    }

    public static Working CreateEmergencyWall()
    {
        var working = new Working(GameStrings.Get("workings.emergency_wall.name"));
        var aim = new WorkingNode(1, "clause.aim_at_target", new Vector2(16, 24));
        var condition = new WorkingNode(2, "clause.if_clear", new Vector2(220, 24));
        var stone = new WorkingNode(3, "clause.raise_stone", new Vector2(430, 24));

        aim.NextNodeId = condition.Id;
        condition.TrueNodeId = stone.Id;

        working.AddNode(aim);
        working.AddNode(condition);
        working.AddNode(stone);
        working.EntryNodeId = aim.Id;

        return working;
    }
}
