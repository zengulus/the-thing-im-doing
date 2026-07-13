using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class WorkingTransactionTests
{
    [Fact]
    public void LateTargetFailure_RollsBackLiveCastAndFailedPreviewForecast()
    {
        var encounter = new TacticalEncounter(9, 5, new GridPos(1, 2));
        EncounterActor visible = encounter.AddDummyEnemy(new GridPos(1, 4), health: 5);
        EncounterActor hidden = encounter.AddDummyEnemy(new GridPos(7, 2), health: 5);
        var wall = new GridPos(4, 2);
        encounter.Grid.SetTile(wall, TileState.Wall);
        Working working = CreateLateFailureWorking();

        WorkingPreview preview = encounter.PreviewWorkingDetailed(working, hidden.Position);

        Assert.False(preview.Result.Succeeded);
        Assert.False(preview.Result.ChangedWorld);
        Assert.Equal("Target is outside line of sight.", preview.Result.FailureReason);
        Assert.Contains(preview.Result.Trace.Events, item =>
            item.Text == "The working failed transactionally; any prior effects were unwound.");
        AssertEncounterUnchanged(preview.Encounter, visible.Id, hidden.Id, wall);
        AssertEncounterUnchanged(encounter, visible.Id, hidden.Id, wall);

        WorkingResult cast = encounter.TryCastWorking(working, hidden.Position);

        Assert.False(cast.Succeeded);
        Assert.False(cast.ChangedWorld);
        Assert.Equal("Target is outside line of sight.", cast.FailureReason);
        Assert.Contains(cast.Trace.Events, item => item.Text == "Damaged actor 2 for 1.");
        Assert.Contains(cast.Trace.Events, item =>
            item.Text == "The selected target is outside line of sight.");
        Assert.Contains(cast.Trace.Events, item =>
            item.Text == "The working failed transactionally; any prior effects were unwound.");
        AssertEncounterUnchanged(encounter, visible.Id, hidden.Id, wall);
    }

    private static void AssertEncounterUnchanged(
        TacticalEncounter encounter,
        int visibleActorId,
        int hiddenActorId,
        GridPos wall)
    {
        EncounterActor visible = encounter.GetActor(visibleActorId)!;
        EncounterActor hidden = encounter.GetActor(hiddenActorId)!;

        Assert.Equal(5, visible.Health);
        Assert.Equal(5, hidden.Health);
        Assert.Empty(visible.Effects);
        Assert.Empty(hidden.Effects);
        Assert.Equal(0, encounter.Player.Counters.Get("counter.bonus.memory"));
        Assert.False(encounter.Player.TryGetWorkingReference("ref.memory.primary", out _));
        Assert.Equal(TileState.Wall, encounter.Grid.GetTile(wall));
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(new GridPos(3, 2)));
        Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);
    }

    private static Working CreateLateFailureWorking()
    {
        var working = new Working("working.test.late_failure", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_nearest_foe") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.damage_them") { NextNodeId = 3 });
        working.AddNode(new WorkingNode(3, "clause.mark_them") { NextNodeId = 4 });
        working.AddNode(new WorkingNode(4, "clause.store_memory_ref") { NextNodeId = 5 });
        working.AddNode(new WorkingNode(5, "clause.aim_at_target"));
        return working;
    }
}
