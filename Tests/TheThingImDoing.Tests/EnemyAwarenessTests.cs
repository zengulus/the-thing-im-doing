using System.Linq;
using TheThingImDoing.Core;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class EnemyAwarenessTests
{
    [Fact]
    public void TacticalGrid_LineOfSightIsBlockedByWallsAndRaisedStone()
    {
        var grid = new TacticalGrid(7, 3);
        var from = new GridPos(0, 1);
        var to = new GridPos(6, 1);

        Assert.True(grid.HasLineOfSight(from, to));

        grid.SetTile(new GridPos(3, 1), TileState.Wall);
        Assert.False(grid.HasLineOfSight(from, to));

        grid.SetTile(new GridPos(3, 1), TileState.Floor);
        grid.SetTile(new GridPos(4, 1), TileState.RaisedStone);
        Assert.False(grid.HasLineOfSight(from, to));
    }

    [Fact]
    public void TacticalGrid_LineOfSightCannotPeekDiagonallyBetweenTwoBlockingCorners()
    {
        var grid = new TacticalGrid(3, 3);
        grid.SetTile(new GridPos(1, 0), TileState.Wall);
        grid.SetTile(new GridPos(0, 1), TileState.RaisedStone);

        Assert.False(grid.HasLineOfSight(new GridPos(0, 0), new GridPos(2, 2)));

        grid.SetTile(new GridPos(0, 1), TileState.Floor);

        Assert.True(grid.HasLineOfSight(new GridPos(0, 0), new GridPos(2, 2)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void TacticalGrid_ShallowDiagonalSupercoverBlocksInBothDirections(int blockerX, int blockerY)
    {
        var grid = new TacticalGrid(3, 2);
        var left = new GridPos(0, 0);
        var right = new GridPos(2, 1);
        grid.SetTile(new GridPos(blockerX, blockerY), TileState.Wall);

        Assert.False(grid.HasLineOfSight(left, right));
        Assert.False(grid.HasLineOfSight(right, left));
    }

    [Fact]
    public void TacticalGrid_LineOfSightIsSymmetricAcrossAllPairsAndSingleBlockers()
    {
        var grid = new TacticalGrid(6, 5);
        GridPos[] positions = Enumerable.Range(0, grid.Height)
            .SelectMany(y => Enumerable.Range(0, grid.Width).Select(x => new GridPos(x, y)))
            .ToArray();

        foreach (GridPos blocker in positions)
        {
            grid.SetTile(blocker, TileState.Wall);

            foreach (GridPos from in positions.Where(position => position != blocker))
            {
                foreach (GridPos to in positions.Where(position => position != blocker))
                {
                    Assert.Equal(grid.HasLineOfSight(from, to), grid.HasLineOfSight(to, from));
                }
            }

            grid.SetTile(blocker, TileState.Floor);
        }
    }

    [Fact]
    public void EnemyAwareness_ClearSightWithinRadiusEngagesFullBehavior()
    {
        int enemyX = 1 + TacticalEncounter.EnemyAwarenessRadius;
        var encounter = new TacticalEncounter(enemyX + 2, 3, new GridPos(1, 1));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(enemyX, 1));

        RunEnemyTurn(encounter);

        Assert.True(hound.IsAlerted);
        Assert.True(encounter.IsEnemyEngaged(hound));
        Assert.Equal(new GridPos(enemyX - 1, 1), hound.Position);
    }

    [Fact]
    public void EnemyAwareness_BeyondViewportRadiusRemainsDormant()
    {
        var encounter = new TacticalEncounter(30, 3, new GridPos(1, 1));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(20, 1));

        RunEnemyTurn(encounter);

        Assert.False(hound.IsAlerted);
        Assert.False(encounter.IsEnemyEngaged(hound));
        Assert.Equal(new GridPos(20, 1), hound.Position);
    }

    [Fact]
    public void EnemyAwareness_AlertedEnemyBeyondRadiusDoesNotRunFullBehavior()
    {
        var encounter = new TacticalEncounter(30, 3, new GridPos(1, 1));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(20, 1));

        encounter.TryDamageActor(hound.Id, 1);
        RunEnemyTurn(encounter);

        Assert.True(hound.IsAlerted);
        Assert.False(encounter.IsEnemyEngaged(hound));
        Assert.Equal(new GridPos(20, 1), hound.Position);
    }

    [Fact]
    public void EnemyAwareness_BlockingTerrainPreventsDirectAlert()
    {
        var encounter = new TacticalEncounter(12, 3, new GridPos(1, 1));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(8, 1));
        encounter.Grid.SetTile(new GridPos(5, 1), TileState.Wall);

        RunEnemyTurn(encounter);

        Assert.False(hound.IsAlerted);
        Assert.Equal(new GridPos(8, 1), hound.Position);
    }

    [Fact]
    public void EnemyAwareness_SuccessfulDamageAlertsEnemyThroughBlockingTerrain()
    {
        var encounter = new TacticalEncounter(12, 3, new GridPos(1, 1));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(8, 1));
        encounter.Grid.SetTile(new GridPos(5, 1), TileState.Wall);

        encounter.TryDamageActor(hound.Id, 1);
        RunEnemyTurn(encounter);

        Assert.True(hound.IsAlerted);
        Assert.NotEqual(new GridPos(8, 1), hound.Position);
    }

    [Fact]
    public void EnemyAwareness_WallBlocksAlertPropagationBetweenAllies()
    {
        var encounter = new TacticalEncounter(12, 5, new GridPos(1, 2));
        EncounterActor scout = encounter.AddEnemy("enemy.glass_hound", new GridPos(5, 2));
        EncounterActor sleeper = encounter.AddEnemy("enemy.glass_hound", new GridPos(8, 2));
        encounter.Grid.SetTile(new GridPos(7, 2), TileState.Wall);

        RunEnemyTurn(encounter);

        Assert.True(scout.IsAlerted);
        Assert.False(sleeper.IsAlerted);
        Assert.Equal(new GridPos(8, 2), sleeper.Position);
    }

    [Fact]
    public void EnemyAwareness_AllyAlertDoesNotPropagateBeyondViewportRadius()
    {
        var encounter = new TacticalEncounter(12, 3, new GridPos(1, 1));
        EncounterActor scout = encounter.AddEnemy("enemy.glass_hound", new GridPos(8, 1));
        EncounterActor distant = encounter.AddEnemy("enemy.glass_hound", new GridPos(9, 1));

        RunEnemyTurn(encounter);

        Assert.True(scout.IsAlerted);
        Assert.False(distant.IsAlerted);
        Assert.Equal(new GridPos(9, 1), distant.Position);
    }

    [Fact]
    public void SporeCantor_CannotApplyRangedPoisonThroughWallAfterAlert()
    {
        var encounter = new TacticalEncounter(8, 3, new GridPos(1, 1));
        EncounterActor cantor = encounter.AddEnemy("enemy.spore_cantor", new GridPos(6, 1));
        encounter.TryDamageActor(cantor.Id, 1);
        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Wall);

        RunEnemyTurn(encounter);

        Assert.Equal(encounter.Player.MaxHealth, encounter.Player.Health);

        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Floor);
        RunEnemyTurn(encounter);

        Assert.Equal(encounter.Player.MaxHealth - 1, encounter.Player.Health);
    }

    [Fact]
    public void AshScribe_CannotBrandThroughWallAfterAlert()
    {
        var encounter = new TacticalEncounter(8, 3, new GridPos(1, 1));
        EncounterActor scribe = encounter.AddEnemy("enemy.ash_scribe", new GridPos(6, 1));
        encounter.TryDamageActor(scribe.Id, 1);
        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Wall);

        RunEnemyTurn(encounter);

        Assert.False(encounter.HasTileCondition(encounter.Player.Position, "condition.marked", scribe.Id));

        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Floor);
        RunEnemyTurn(encounter);

        Assert.True(encounter.HasTileCondition(encounter.Player.Position, "condition.marked", scribe.Id));
    }

    [Fact]
    public void ObsidianCrown_CannotBrandThroughWallButStillPathsTowardPlayer()
    {
        var encounter = new TacticalEncounter(8, 3, new GridPos(1, 1));
        EncounterActor crown = encounter.AddEnemy("enemy.obsidian_crown", new GridPos(6, 1));
        encounter.TryDamageActor(crown.Id, 1);
        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Wall);
        GridPos initialPosition = crown.Position;

        RunEnemyTurn(encounter);

        Assert.False(encounter.HasTileCondition(encounter.Player.Position, "condition.marked", crown.Id));
        Assert.NotEqual(initialPosition, crown.Position);

        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Floor);
        RunEnemyTurn(encounter);

        Assert.True(encounter.HasTileCondition(encounter.Player.Position, "condition.marked", crown.Id));
    }

    private static void RunEnemyTurn(TacticalEncounter encounter)
    {
        encounter.WaitPlayerTurn();
        encounter.RunEnemyTurn();
    }
}
