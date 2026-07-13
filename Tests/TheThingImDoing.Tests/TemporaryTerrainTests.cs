using System.Linq;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class TemporaryTerrainTests
{
    [Fact]
    public void WorkingRaisedStone_ExpiresAfterThreeEnemyTurns()
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(6, 1), health: 20);
        var stonePosition = new GridPos(3, 1);

        WorkingResult cast = encounter.TryCastWorking(WorkingSamples.CreateEmergencyWall(), stonePosition);

        Assert.True(cast.Succeeded);
        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(stonePosition));

        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(stonePosition));

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(stonePosition));

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(stonePosition));
    }

    [Fact]
    public void AuthoredRaisedStone_IsPermanent()
    {
        var encounter = new TacticalEncounter(
            8,
            5,
            new GridPos(1, 2),
            "rule.echoing_steps",
            playerHealth: 20,
            playerMaxHealth: 20);
        encounter.AddDummyEnemy(new GridPos(7, 2), health: 20);
        var stonePosition = new GridPos(3, 2);
        encounter.Grid.SetTile(stonePosition, TileState.RaisedStone);

        for (int i = 0; i < TacticalEncounter.TemporaryRaisedStoneDuration + 1; i++)
        {
            encounter.WaitPlayerTurn();
            ResolveEnemyTurn(encounter);
        }

        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(stonePosition));
    }

    [Fact]
    public void RootSaintRaisedStone_UsesTheSameTemporaryLifetime()
    {
        var encounter = new TacticalEncounter(8, 5, new GridPos(1, 2));
        EncounterActor root = encounter.AddEnemy("enemy.root_saint", new GridPos(5, 2));
        encounter.TryDamageActor(root.Id, 1);

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);

        GridPos firstStone = TacticalEncounter.GetAdjacentPositions(root.Position)
            .Single(position => encounter.Grid.GetTile(position) == TileState.RaisedStone);

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(firstStone));

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(firstStone));
    }

    [Fact]
    public void RaisingSameStoneAgain_RefreshesItsLifetime()
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(6, 1), health: 20);
        var stonePosition = new GridPos(3, 1);

        Assert.True(encounter.TrySetTileState(stonePosition, TileState.RaisedStone));
        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);
        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);

        Assert.True(encounter.TrySetTileState(stonePosition, TileState.RaisedStone));

        for (int i = 0; i < TacticalEncounter.TemporaryRaisedStoneDuration - 1; i++)
        {
            encounter.WaitPlayerTurn();
            ResolveEnemyTurn(encounter);
        }

        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(stonePosition));

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(stonePosition));
    }

    [Fact]
    public void DirectRaiseStoneClause_CannotReplaceWallOrTriggerSpellHooks()
    {
        var encounter = new TacticalEncounter(
            7,
            3,
            new GridPos(1, 1),
            "rule.obsidian_resonance",
            playerHealth: 5,
            playerMaxHealth: 5);
        encounter.AddDummyEnemy(new GridPos(6, 1), health: 20);
        var wall = new GridPos(3, 1);
        encounter.Grid.SetTile(wall, TileState.Wall);

        WorkingResult result = encounter.TryCastWorking(CreateDirectRaiseWorking(), wall);

        Assert.True(result.Succeeded);
        Assert.False(result.ChangedWorld);
        Assert.Equal(TileState.Wall, encounter.Grid.GetTile(wall));
        Assert.Equal(encounter.Player.MaxHealth, encounter.Player.Health);
        Assert.Null(encounter.Player.FindEffect("effect.ward", encounter.Player.Id));
    }

    [Fact]
    public void DirectRaiseStoneClause_CannotMakeAuthoredStoneTemporary()
    {
        var encounter = new TacticalEncounter(
            7,
            5,
            new GridPos(1, 2),
            "rule.echoing_steps",
            playerHealth: 20,
            playerMaxHealth: 20);
        encounter.AddDummyEnemy(new GridPos(6, 2), health: 20);
        var authoredStone = new GridPos(3, 2);
        encounter.Grid.SetTile(authoredStone, TileState.RaisedStone);

        WorkingResult result = encounter.TryCastWorking(CreateDirectRaiseWorking(), authoredStone);
        Assert.True(result.Succeeded);
        Assert.False(result.ChangedWorld);
        ResolveEnemyTurn(encounter);

        for (int turn = 0; turn < TacticalEncounter.TemporaryRaisedStoneDuration + 1; turn++)
        {
            encounter.WaitPlayerTurn();
            ResolveEnemyTurn(encounter);
        }

        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(authoredStone));
    }

    [Fact]
    public void DirectRaiseStoneClause_CannotRefreshTemporaryStone()
    {
        var encounter = new TacticalEncounter(
            7,
            3,
            new GridPos(1, 1),
            "rule.echoing_steps",
            playerHealth: 20,
            playerMaxHealth: 20);
        encounter.AddDummyEnemy(new GridPos(6, 1), health: 20);
        var stone = new GridPos(3, 1);
        Assert.True(encounter.TrySetTileState(stone, TileState.RaisedStone));

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);

        WorkingResult repeated = encounter.TryCastWorking(CreateDirectRaiseWorking(), stone);
        Assert.True(repeated.Succeeded);
        Assert.False(repeated.ChangedWorld);
        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(stone));

        encounter.WaitPlayerTurn();
        ResolveEnemyTurn(encounter);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(stone));
    }

    private static void ResolveEnemyTurn(TacticalEncounter encounter)
    {
        Assert.Equal(TurnPhase.EnemyTurn, encounter.Turns.Phase);
        encounter.RunEnemyTurn();
    }

    private static Working CreateDirectRaiseWorking()
    {
        var working = new Working("working.test.direct_raise", "workings.emergency_wall.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.raise_stone"));
        return working;
    }
}
