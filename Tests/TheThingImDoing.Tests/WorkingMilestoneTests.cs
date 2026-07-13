using System.Linq;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class WorkingMilestoneTests
{
    [Fact]
    public void MarkOrSpark_FirstExecution_MarksNearestEnemy()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);

        WorkingResult result = ExecuteWorking(encounter, WorkingSamples.CreateMarkOrDamage(), enemy.Position);

        Assert.True(result.Succeeded);
        Assert.True(result.ChangedWorld);
        Assert.True(encounter.HasActorCondition(enemy.Id, "condition.marked", encounter.Player.Id));
        Assert.Equal(3, enemy.Health);
    }

    [Fact]
    public void MarkOrSpark_SecondExecution_DamagesMarkedEnemy()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);
        Working working = WorkingSamples.CreateMarkOrDamage();

        ExecuteWorking(encounter, working, enemy.Position);
        WorkingResult result = ExecuteWorking(encounter, working, enemy.Position);

        Assert.True(result.Succeeded);
        Assert.Equal(2, enemy.Health);
        Assert.Contains(result.Trace.Events, traceEvent => traceEvent.Text.Contains("Damaged actor"));
    }

    [Fact]
    public void EmergencyWall_ClearTile_RaisesStone()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        var target = new GridPos(3, 3);

        WorkingResult result = ExecuteWorking(encounter, WorkingSamples.CreateEmergencyWall(), target);

        Assert.True(result.Succeeded);
        Assert.True(result.ChangedWorld);
        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(target));
    }

    [Fact]
    public void EmergencyWall_OccupiedTile_LeavesBoardUnchangedAndExplainsCondition()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);

        WorkingResult result = ExecuteWorking(encounter, WorkingSamples.CreateEmergencyWall(), enemy.Position);

        Assert.True(result.Succeeded);
        Assert.False(result.ChangedWorld);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(enemy.Position));
        Assert.Contains(result.Trace.Events, traceEvent =>
            traceEvent.Text.Contains("\"if clear\"") && traceEvent.Text.Contains("failed"));
    }

    [Fact]
    public void PushTrap_IntoRaisedStone_BreaksStoneDamagesActorAndTracesCollision()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(2, 1), health: 2);
        var stone = new GridPos(3, 1);
        encounter.Grid.SetTile(stone, TileState.RaisedStone);

        WorkingResult result = ExecuteWorking(encounter, CreatePushTrap(), enemy.Position);

        Assert.True(result.Succeeded);
        Assert.True(result.ChangedWorld);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(stone));
        Assert.Equal(1, enemy.Health);
        Assert.Equal(new GridPos(2, 1), enemy.Position);
        Assert.Contains(result.Trace.Events, traceEvent => traceEvent.Text.Contains("to Floor"));
        Assert.Contains(result.Trace.Events, traceEvent => traceEvent.Text.Contains("Damaged actor"));
    }

    [Fact]
    public void PreviewWorkingDetailed_DoesNotMutateOriginalAndMatchesCastResult()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);
        Working working = WorkingSamples.CreateMarkOrDamage();

        WorkingPreview preview = encounter.PreviewWorkingDetailed(working, enemy.Position);

        Assert.True(preview.Result.Succeeded);
        Assert.False(encounter.HasActorCondition(enemy.Id, "condition.marked", encounter.Player.Id));
        Assert.True(preview.Encounter.HasActorCondition(enemy.Id, "condition.marked", preview.Encounter.Player.Id));

        WorkingResult cast = encounter.TryCastWorking(working, enemy.Position);

        Assert.True(cast.Succeeded);
        Assert.True(encounter.HasActorCondition(enemy.Id, "condition.marked", encounter.Player.Id));
        Assert.Equal(
            preview.Encounter.GetActor(enemy.Id)?.Effects.Select(effect => effect.EffectId),
            encounter.GetActor(enemy.Id)?.Effects.Select(effect => effect.EffectId));
    }

    private static WorkingResult ExecuteWorking(TacticalEncounter encounter, Working working, GridPos selectedTarget)
    {
        return new WorkingMachine().Execute(
            working,
            new EncounterSpellWorld(encounter),
            encounter.Player.Id,
            selectedTarget);
    }

    private static Working CreatePushTrap()
    {
        var working = new Working("working.push_trap", "Push Trap");
        var aim = new WorkingNode(1, "clause.aim_at_target");
        var occupied = new WorkingNode(2, "clause.if_occupied");
        var push = new WorkingNode(3, "clause.push_them");

        aim.NextNodeId = occupied.Id;
        occupied.TrueNodeId = push.Id;

        working.AddNode(aim);
        working.AddNode(occupied);
        working.AddNode(push);
        working.EntryNodeId = aim.Id;
        return working;
    }
}
