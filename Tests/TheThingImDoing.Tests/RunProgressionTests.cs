using System.Linq;
using TheThingImDoing.Core;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class RunProgressionTests
{
    [Fact]
    public void BaseRunContent_ResolvesAllCrossReferences()
    {
        EnvironmentDefinition environment = EnvironmentDefinitionCatalog.Get("environment.ashen_archive");
        RunDefinition run = RunDefinitionCatalog.Get("run.ashen_archive");
        EnvironmentDefinition[] environments = EnvironmentDefinitionCatalog.All.ToArray();
        EncounterDefinition[] encounters = EncounterDefinitionCatalog.All.ToArray();

        Assert.Equal("rule.brittle_stone", environment.FloorRuleId);
        Assert.Equal("Ashen Archive", environment.DisplayName);
        Assert.True(environments.Length >= 4);
        Assert.True(encounters.Length >= 5);
        Assert.Equal(5, run.EncounterIds.Count);
        Assert.Equal(4, encounters.Select(encounter => encounter.EnvironmentId).Distinct().Count());

        for (int index = 0; index < run.EncounterIds.Count; index++)
        {
            EncounterDefinition encounter = EncounterDefinitionCatalog.Get(run.EncounterIds[index]);

            Assert.True(EnvironmentDefinitionCatalog.TryGet(encounter.EnvironmentId, out _));
            Assert.NotEmpty(encounter.Enemies);
            Assert.Equal(index == run.EncounterIds.Count - 1, encounter.IsFinal);
            Assert.All(encounter.Enemies, placement =>
                Assert.True(Actors.EnemyConfigCatalog.TryGet(placement.EnemyId, out _)));
        }

        EncounterDefinition finalEncounter = EncounterDefinitionCatalog.Get(run.EncounterIds[^1]);
        Assert.Contains(
            finalEncounter.Enemies,
            placement => placement.EnemyId == "enemy.obsidian_crown");
    }

    [Fact]
    public void Factory_BuildsGridRuleTilesEnemiesAndCarriedPlayerHealth()
    {
        EncounterDefinition definition = EncounterDefinitionCatalog.Get("encounter.ashen_threshold");
        TacticalEncounter encounter = TacticalEncounterFactory.Create(
            definition,
            playerHealth: 3,
            playerMaxHealth: 5);

        Assert.NotNull(definition.Generation);
        Assert.Equal(definition.Generation!.Width, encounter.Grid.Width);
        Assert.Equal(definition.Generation.Height, encounter.Grid.Height);
        Assert.True(encounter.Grid.IsInside(encounter.Player.Position));
        Assert.False(encounter.Grid.IsBlocked(encounter.Player.Position));
        Assert.Equal(3, encounter.Player.Health);
        Assert.Equal(5, encounter.Player.MaxHealth);
        Assert.Equal("rule.brittle_stone", encounter.FloorRules.ActiveRuleId);
        TileState[] tileStates = Enumerable.Range(0, encounter.Grid.Height)
            .SelectMany(y => Enumerable.Range(0, encounter.Grid.Width)
                .Select(x => encounter.Grid.GetTile(new GridPos(x, y))))
            .ToArray();
        Assert.Contains(TileState.Wall, tileStates);
        Assert.Contains(TileState.RaisedStone, tileStates);

        EncounterActor[] enemies = encounter.Enemies.ToArray();
        Assert.Equal(definition.Generation.EnemyCount, enemies.Length);
        Assert.All(enemies, enemy =>
        {
            Assert.Equal("enemy.glass_hound", enemy.EnemyId);
            Assert.True(encounter.Grid.IsInside(enemy.Position));
            Assert.False(encounter.Grid.IsBlocked(enemy.Position));
            Assert.NotEqual(encounter.Player.Position, enemy.Position);
            Assert.Equal(4, enemy.MaxHealth);
        });
    }

    [Fact]
    public void ObsidianCrownEncounter_BossRoutesAroundAuthoredWallToReachPlayer()
    {
        EncounterDefinition definition = EncounterDefinitionCatalog.Get("encounter.obsidian_crown");
        EnvironmentDefinition environment = EnvironmentDefinitionCatalog.Get(definition.EnvironmentId);
        var encounter = new TacticalEncounter(
            definition.GridWidth,
            definition.GridHeight,
            definition.PlayerStart,
            environment.FloorRuleId,
            playerHealth: 100,
            playerMaxHealth: 100);

        foreach (EncounterTilePlacement tile in definition.Tiles)
        {
            encounter.Grid.SetTile(tile.Position, tile.State);
        }

        foreach (EncounterEnemyPlacement enemy in definition.Enemies)
        {
            encounter.AddEnemy(enemy.EnemyId, enemy.Position);
        }

        EncounterActor crown = encounter.Enemies.Single(enemy => enemy.EnemyId == "enemy.obsidian_crown");

        foreach (EncounterActor support in encounter.Enemies.Where(enemy => enemy.Id != crown.Id).ToArray())
        {
            encounter.TryDamageActor(support.Id, support.MaxHealth);
        }

        int initialDistance = crown.Position.ManhattanDistanceTo(encounter.Player.Position);
        encounter.TryDamageActor(crown.Id, 1);

        for (int turn = 0; turn < 16; turn++)
        {
            encounter.WaitPlayerTurn();
            encounter.RunEnemyTurn();
        }

        Assert.True(crown.Position.ManhattanDistanceTo(encounter.Player.Position) < initialDistance);
        Assert.Equal(1, crown.Position.ManhattanDistanceTo(encounter.Player.Position));
    }

    [Fact]
    public void Session_OnlyAdvancesOnVictoryAndCompletesOrderedRun()
    {
        var session = new GameRunSession("run.ashen_archive");

        Assert.Equal("encounter.ashen_threshold", session.CurrentEncounterId);
        Assert.False(session.TryAdvance(GameResult.InProgress));
        Assert.False(session.TryAdvance(GameResult.PlayerLost));
        Assert.Equal(0, session.Victories);

        while (!session.IsComplete)
        {
            string encounterId = session.CurrentEncounterId!;
            TacticalEncounter encounter = TacticalEncounterFactory.Create(encounterId);

            foreach (EncounterActor enemy in encounter.Enemies.ToArray())
            {
                encounter.TryDamageActor(enemy.Id, enemy.MaxHealth);
            }

            Assert.Equal(GameResult.PlayerWon, encounter.Result);
            Assert.True(session.TryAdvance(encounter.Result));
        }

        Assert.Equal(5, session.Victories);
        Assert.Equal(5, session.CurrentEncounterIndex);
        Assert.Null(session.CurrentEncounterId);
        Assert.Null(session.CurrentEncounter);
        Assert.False(session.TryAdvance(GameResult.PlayerWon));
    }

    [Fact]
    public void Session_DerivesStableDistinctFloorSeedsFromRunSeed()
    {
        var first = new GameRunSession("run.ashen_archive", seed: 4242);
        var same = new GameRunSession("run.ashen_archive", seed: 4242);
        var different = new GameRunSession("run.ashen_archive", seed: 4243);

        Assert.Equal(first.CurrentEncounterSeed, same.CurrentEncounterSeed);
        Assert.NotEqual(first.CurrentEncounterSeed, different.CurrentEncounterSeed);

        int firstFloorSeed = first.CurrentEncounterSeed!.Value;
        first.TryAdvance(GameResult.PlayerWon);

        Assert.NotEqual(firstFloorSeed, first.CurrentEncounterSeed);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(6, 5)]
    [InlineData(1, 0)]
    public void TacticalEncounter_RejectsInvalidPlayerHealth(int health, int maxHealth)
    {
        Assert.ThrowsAny<System.ArgumentOutOfRangeException>(() =>
            new TacticalEncounter(
                5,
                5,
                new GridPos(1, 1),
                "rule.brittle_stone",
                health,
                maxHealth));
    }
}
