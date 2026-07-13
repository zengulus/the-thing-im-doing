using System;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class SpellTargetingTests
{
    [Fact]
    public void SelectedTarget_BeyondPerceptionRange_FailsPreviewAndCastWithoutSpendingTurn()
    {
        var encounter = new TacticalEncounter(30, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(20, 1), health: 3);
        Working working = CreateSelectedTargetDamageWorking();

        WorkingResult preview = encounter.PreviewWorking(working, enemy.Position);
        WorkingResult cast = encounter.TryCastWorking(working, enemy.Position);

        Assert.False(preview.Succeeded);
        Assert.Equal("Target is beyond perception range.", preview.FailureReason);
        Assert.False(cast.Succeeded);
        Assert.Equal(preview.FailureReason, cast.FailureReason);
        Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);
        Assert.Equal(3, enemy.Health);
        Assert.Contains(cast.Trace.Events, item =>
            item.Text.Contains("beyond perception range", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(TileState.Wall)]
    [InlineData(TileState.RaisedStone)]
    public void SelectedTarget_BehindBlockingTerrain_FailsWithoutSpendingTurn(TileState blocker)
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(5, 1), health: 3);
        encounter.Grid.SetTile(new GridPos(3, 1), blocker);

        WorkingResult preview = encounter.PreviewWorking(CreateSelectedTargetDamageWorking(), enemy.Position);
        WorkingResult cast = encounter.TryCastWorking(CreateSelectedTargetDamageWorking(), enemy.Position);

        Assert.False(preview.Succeeded);
        Assert.Equal("Target is outside line of sight.", preview.FailureReason);
        Assert.False(cast.Succeeded);
        Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);
        Assert.Equal(3, enemy.Health);
    }

    [Fact]
    public void SelectedTarget_InRangeWithClearLineOfSight_ResolvesAndSpendsTurn()
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(5, 1), health: 3);

        WorkingResult result = encounter.TryCastWorking(CreateSelectedTargetDamageWorking(), enemy.Position);

        Assert.True(result.Succeeded);
        Assert.Equal(2, enemy.Health);
        Assert.Equal(TurnPhase.EnemyTurn, encounter.Turns.Phase);
    }

    [Fact]
    public void NearestFoe_IgnoresCloserHiddenEnemyAndTargetsVisibleEnemy()
    {
        var encounter = new TacticalEncounter(8, 8, new GridPos(1, 3));
        EncounterActor hidden = encounter.AddDummyEnemy(new GridPos(3, 3), health: 3);
        EncounterActor visible = encounter.AddDummyEnemy(new GridPos(1, 6), health: 3);
        encounter.Grid.SetTile(new GridPos(2, 3), TileState.Wall);

        WorkingResult result = encounter.TryCastWorking(CreateNearestTargetDamageWorking(), encounter.Player.Position);

        Assert.True(result.Succeeded);
        Assert.Equal(3, hidden.Health);
        Assert.Equal(2, visible.Health);
    }

    [Fact]
    public void NearestFoe_WithNoVisibleEnemy_FailsWithoutSpendingTurn()
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(5, 1), health: 3);
        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Wall);

        WorkingResult result = encounter.TryCastWorking(CreateNearestTargetDamageWorking(), enemy.Position);

        Assert.False(result.Succeeded);
        Assert.Equal("No visible target is within perception range.", result.FailureReason);
        Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);
        Assert.Equal(3, enemy.Health);
    }

    private static Working CreateSelectedTargetDamageWorking()
    {
        var working = new Working("working.test.damage_selected", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.damage_them"));
        return working;
    }

    private static Working CreateNearestTargetDamageWorking()
    {
        var working = new Working("working.test.damage_nearest", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_nearest_foe") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.damage_them"));
        return working;
    }
}
