using Godot;

namespace TheThingImDoing.Spells;

public static class WorkingSamples
{
    public static Working CreateMarkOrDamage()
    {
        var working = new Working("working.mark_or_damage", "workings.mark_or_damage.name");
        var aim = new WorkingNode(1, "clause.aim_at_nearest_foe");
        var condition = new WorkingNode(2, "clause.if_marked");
        var damage = new WorkingNode(3, "clause.damage_them");
        var mark = new WorkingNode(4, "clause.mark_them");

        aim.NextNodeId = condition.Id;
        condition.TrueNodeId = damage.Id;
        condition.FalseNodeId = mark.Id;

        working.AddNode(aim);
        working.AddNode(condition);
        working.AddNode(damage);
        working.AddNode(mark);
        working.EntryNodeId = aim.Id;
        SetPosition(working, aim.Id, new Vector2(16, 24));
        SetPosition(working, condition.Id, new Vector2(260, 24));
        SetPosition(working, damage.Id, new Vector2(504, 4));
        SetPosition(working, mark.Id, new Vector2(504, 140));

        return working;
    }

    public static Working CreateEmergencyWall()
    {
        var working = new Working("working.emergency_wall", "workings.emergency_wall.name");
        var aim = new WorkingNode(1, "clause.aim_at_target");
        var condition = new WorkingNode(2, "clause.if_clear");
        var stone = new WorkingNode(3, "clause.raise_stone");

        aim.NextNodeId = condition.Id;
        condition.TrueNodeId = stone.Id;

        working.AddNode(aim);
        working.AddNode(condition);
        working.AddNode(stone);
        working.EntryNodeId = aim.Id;
        SetPosition(working, aim.Id, new Vector2(16, 24));
        SetPosition(working, condition.Id, new Vector2(260, 24));
        SetPosition(working, stone.Id, new Vector2(504, 24));

        return working;
    }

    private static void SetPosition(Working working, int nodeId, Vector2 position)
    {
        working.SetNodeLayout(nodeId, new WorkingNodeLayout(position.X, position.Y));
    }
}
