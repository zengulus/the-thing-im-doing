using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Core;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class ProceduralEncounterGeneratorTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalLayout()
    {
        EncounterDefinition template = EncounterDefinitionCatalog.Get("encounter.verdant_choir");

        EncounterLayout first = ProceduralEncounterGenerator.Generate(template, seedOverride: 73421);
        EncounterLayout second = ProceduralEncounterGenerator.Generate(template, seedOverride: 73421);

        Assert.Equal(first.Width, second.Width);
        Assert.Equal(first.Height, second.Height);
        Assert.Equal(first.PlayerStart, second.PlayerStart);
        Assert.Equal(first.Rooms, second.Rooms);
        Assert.Equal(first.Tiles, second.Tiles);
        Assert.Equal(first.Enemies, second.Enemies);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentLayouts()
    {
        EncounterDefinition template = EncounterDefinitionCatalog.Get("encounter.verdant_choir");

        EncounterLayout first = ProceduralEncounterGenerator.Generate(template, seedOverride: 1001);
        EncounterLayout second = ProceduralEncounterGenerator.Generate(template, seedOverride: 1002);

        Assert.NotEqual(LayoutSignature(first), LayoutSignature(second));
    }

    [Theory]
    [InlineData("encounter.ashen_threshold", 4)]
    [InlineData("encounter.echoing_causeway", 6)]
    [InlineData("encounter.verdant_choir", 7)]
    [InlineData("encounter.obsidian_approach", 8)]
    [InlineData("encounter.obsidian_crown", 10)]
    public void ConfiguredEnemyCount_ExpandsAuthoredRosterDeterministically(
        string encounterId,
        int expectedEnemyCount)
    {
        EncounterDefinition template = EncounterDefinitionCatalog.Get(encounterId);
        EncounterLayout first = ProceduralEncounterGenerator.Generate(template);
        EncounterLayout second = ProceduralEncounterGenerator.Generate(template);

        Assert.NotNull(template.Generation);
        Assert.Equal(expectedEnemyCount, template.Generation!.EnemyCount);
        Assert.Equal(expectedEnemyCount, first.Enemies.Count);
        Assert.Equal(first.Enemies, second.Enemies);
        Assert.Equal(
            template.Enemies.Select(enemy => enemy.EnemyId),
            first.Enemies.Take(template.Enemies.Count).Select(enemy => enemy.EnemyId));
        Assert.All(
            template.Enemies.Select(enemy => enemy.EnemyId).Distinct(),
            enemyId => Assert.Contains(first.Enemies, enemy => enemy.EnemyId == enemyId));
        Assert.Contains(
            first.Enemies,
            enemy => System.Math.Max(
                    System.Math.Abs(enemy.Position.X - first.PlayerStart.X),
                    System.Math.Abs(enemy.Position.Y - first.PlayerStart.Y))
                > TacticalEncounter.EnemyAwarenessRadius);
    }

    [Fact]
    public void FinalRoster_ContainsExactlyOneCrownAndExpandsOnlySupportEnemies()
    {
        EncounterDefinition template = EncounterDefinitionCatalog.Get("encounter.obsidian_crown");

        EncounterLayout layout = ProceduralEncounterGenerator.Generate(template, seedOverride: 5505);

        Assert.Equal(10, layout.Enemies.Count);
        Assert.Single(layout.Enemies, enemy => enemy.EnemyId == "enemy.obsidian_crown");
        Assert.All(
            layout.Enemies.Skip(template.Enemies.Count),
            enemy => Assert.NotEqual("enemy.obsidian_crown", enemy.EnemyId));
    }

    [Fact]
    public void ObjectiveEnemy_IsNeverRepeatedEvenWithoutBossTags()
    {
        EncounterDefinition sandbox = EncounterDefinitionCatalog.Get("encounter.archive_expanse");
        var untaggedObjective = sandbox with
        {
            VictoryTargetEnemyId = "enemy.glass_hound"
        };

        EncounterLayout layout = ProceduralEncounterGenerator.Generate(
            untaggedObjective,
            seedOverride: 4242);

        Assert.Single(layout.Enemies, enemy => enemy.EnemyId == "enemy.glass_hound");
        Assert.Equal(untaggedObjective.Generation!.EnemyCount, layout.Enemies.Count);
    }

    [Fact]
    public void GeneratorApi_RejectsMissingOrDuplicateObjectivePlacements()
    {
        EncounterDefinition sandbox = EncounterDefinitionCatalog.Get("encounter.archive_expanse");
        var missing = sandbox with
        {
            Enemies = sandbox.Enemies
                .Where(enemy => enemy.EnemyId != sandbox.VictoryTargetEnemyId)
                .ToArray()
        };
        var duplicate = sandbox with
        {
            Enemies =
            [
                .. sandbox.Enemies,
                new EncounterEnemyPlacement(
                    sandbox.VictoryTargetEnemyId!,
                    new GridPos(2, 2))
            ]
        };

        Assert.Contains(
            ProceduralEncounterGenerator.Validate(missing),
            issue => issue.Contains("found 0"));
        Assert.Contains(
            ProceduralEncounterGenerator.Validate(duplicate),
            issue => issue.Contains("found 2"));
        Assert.Throws<System.ArgumentException>(() =>
            ProceduralEncounterGenerator.Generate(missing, seedOverride: 1));
        Assert.Throws<System.ArgumentException>(() =>
            ProceduralEncounterGenerator.Generate(duplicate, seedOverride: 1));
    }

    [Fact]
    public void BossOnlyRoster_CannotExpandBeyondItsAuthoredCount()
    {
        EncounterDefinition final = EncounterDefinitionCatalog.Get("encounter.obsidian_crown");
        var bossOnly = final with
        {
            Enemies = final.Enemies
                .Where(enemy => enemy.EnemyId == "enemy.obsidian_crown")
                .ToArray(),
            Generation = final.Generation! with { EnemyCount = 2 }
        };

        IReadOnlyList<string> issues = ProceduralEncounterGenerator.Validate(bossOnly);

        Assert.Contains(issues, issue => issue.Contains("no non-objective, non-boss enemy"));
        Assert.Throws<System.ArgumentException>(() =>
            ProceduralEncounterGenerator.Generate(bossOnly, seedOverride: 1));
    }

    [Theory]
    [InlineData("encounter.ashen_threshold")]
    [InlineData("encounter.echoing_causeway")]
    [InlineData("encounter.verdant_choir")]
    [InlineData("encounter.obsidian_approach")]
    [InlineData("encounter.obsidian_crown")]
    public void GeneratedFloors_AreConnectedAndBounded(string encounterId)
    {
        EncounterDefinition template = EncounterDefinitionCatalog.Get(encounterId);
        EncounterLayout layout = ProceduralEncounterGenerator.Generate(template);
        HashSet<GridPos> blocked = layout.Tiles
            .Where(tile => tile.State.IsBlocking())
            .Select(tile => tile.Position)
            .ToHashSet();
        HashSet<GridPos> floor = AllPositions(layout)
            .Where(position => !blocked.Contains(position))
            .ToHashSet();
        HashSet<GridPos> reachable = FloodFill(layout.PlayerStart, floor);

        Assert.True(layout.Width >= ProceduralEncounterGenerator.MinimumWidth);
        Assert.True(layout.Height >= ProceduralEncounterGenerator.MinimumHeight);
        Assert.NotEmpty(layout.Rooms);
        Assert.NotEmpty(floor);
        Assert.Equal(floor.Count, reachable.Count);
        Assert.All(layout.Tiles, tile => Assert.True(IsInside(tile.Position, layout)));
        Assert.All(layout.Rooms, room =>
        {
            Assert.True(IsInside(new GridPos(room.X, room.Y), layout));
            Assert.True(IsInside(new GridPos(room.X + room.Width - 1, room.Y + room.Height - 1), layout));
        });

        for (int x = 0; x < layout.Width; x++)
        {
            Assert.Contains(new GridPos(x, 0), blocked);
            Assert.Contains(new GridPos(x, layout.Height - 1), blocked);
        }

        for (int y = 0; y < layout.Height; y++)
        {
            Assert.Contains(new GridPos(0, y), blocked);
            Assert.Contains(new GridPos(layout.Width - 1, y), blocked);
        }
    }

    [Theory]
    [InlineData("encounter.ashen_threshold", 91)]
    [InlineData("encounter.verdant_choir", 92)]
    [InlineData("encounter.obsidian_crown", 93)]
    public void ActorSpawns_AreUniqueWalkableReachableAndPreserveTemplateRoster(
        string encounterId,
        int seed)
    {
        EncounterDefinition template = EncounterDefinitionCatalog.Get(encounterId);
        EncounterLayout layout = ProceduralEncounterGenerator.Generate(template, seed);
        HashSet<GridPos> blocked = layout.Tiles
            .Where(tile => tile.State.IsBlocking())
            .Select(tile => tile.Position)
            .ToHashSet();
        GridPos[] actorPositions = layout.Enemies
            .Select(enemy => enemy.Position)
            .Prepend(layout.PlayerStart)
            .ToArray();

        Assert.NotNull(template.Generation);
        Assert.Equal(template.Generation!.EnemyCount, layout.Enemies.Count);
        Assert.Equal(
            template.Enemies.Select(enemy => enemy.EnemyId),
            layout.Enemies.Take(template.Enemies.Count).Select(enemy => enemy.EnemyId));
        Assert.All(
            template.Enemies.Select(enemy => enemy.EnemyId).Distinct(),
            enemyId => Assert.Contains(layout.Enemies, enemy => enemy.EnemyId == enemyId));
        Assert.Equal(actorPositions.Length, actorPositions.Distinct().Count());
        Assert.All(actorPositions, position =>
        {
            Assert.True(IsInside(position, layout));
            Assert.DoesNotContain(position, blocked);
        });

        HashSet<GridPos> floor = AllPositions(layout)
            .Where(position => !blocked.Contains(position))
            .ToHashSet();
        HashSet<GridPos> reachable = FloodFill(layout.PlayerStart, floor);
        Assert.All(actorPositions, position => Assert.Contains(position, reachable));
        ProceduralRoom startRoom = layout.Rooms[0];
        Assert.DoesNotContain(
            layout.Enemies,
            enemy => enemy.Position.X >= startRoom.X
                && enemy.Position.X < startRoom.X + startRoom.Width
                && enemy.Position.Y >= startRoom.Y
                && enemy.Position.Y < startRoom.Y + startRoom.Height);
        HashSet<GridPos> nonStartRoomCenters = layout.Rooms
            .Skip(1)
            .Select(room => room.Center)
            .ToHashSet();
        int anchoredEnemyCount = System.Math.Min(layout.Enemies.Count, nonStartRoomCenters.Count);
        Assert.All(
            layout.Enemies.Take(anchoredEnemyCount),
            enemy => Assert.Contains(enemy.Position, nonStartRoomCenters));
        Assert.Equal(
            anchoredEnemyCount,
            layout.Enemies.Take(anchoredEnemyCount).Select(enemy => enemy.Position).Distinct().Count());
    }

    [Fact]
    public void Factory_UsesProceduralConfigurationButKeepsAuthoredDefinitionAsTemplate()
    {
        EncounterDefinition template = EncounterDefinitionCatalog.Get("encounter.ashen_threshold");
        TacticalEncounter first = TacticalEncounterFactory.Create(
            template,
            playerHealth: 3,
            playerMaxHealth: 5,
            proceduralSeed: 8181);
        TacticalEncounter second = TacticalEncounterFactory.Create(
            template,
            playerHealth: 3,
            playerMaxHealth: 5,
            proceduralSeed: 8181);

        Assert.Equal(6, template.GridWidth);
        Assert.Equal(5, template.GridHeight);
        Assert.Equal(new GridPos(1, 2), template.PlayerStart);
        Assert.NotNull(template.Generation);
        Assert.Equal(template.Generation!.Width, first.Grid.Width);
        Assert.Equal(template.Generation.Height, first.Grid.Height);
        Assert.Equal(template.Generation.EnemyCount, first.Enemies.Count());
        Assert.Equal(first.Player.Position, second.Player.Position);
        Assert.Equal(3, first.Player.Health);
        Assert.Equal(5, first.Player.MaxHealth);
        Assert.Equal("rule.brittle_stone", first.FloorRules.ActiveRuleId);
        Assert.Equal(
            GridSignature(first.Grid),
            GridSignature(second.Grid));
    }

    private static HashSet<GridPos> FloodFill(GridPos start, IReadOnlySet<GridPos> floor)
    {
        var visited = new HashSet<GridPos>();
        var pending = new Queue<GridPos>();
        pending.Enqueue(start);

        GridPos[] offsets =
        [
            new GridPos(1, 0),
            new GridPos(-1, 0),
            new GridPos(0, 1),
            new GridPos(0, -1)
        ];

        while (pending.TryDequeue(out GridPos position))
        {
            if (!floor.Contains(position) || !visited.Add(position))
            {
                continue;
            }

            foreach (GridPos offset in offsets)
            {
                pending.Enqueue(position + offset);
            }
        }

        return visited;
    }

    private static IEnumerable<GridPos> AllPositions(EncounterLayout layout)
    {
        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                yield return new GridPos(x, y);
            }
        }
    }

    private static bool IsInside(GridPos position, EncounterLayout layout)
    {
        return position.X >= 0
            && position.X < layout.Width
            && position.Y >= 0
            && position.Y < layout.Height;
    }

    private static string LayoutSignature(EncounterLayout layout)
    {
        return string.Join(
            '|',
            layout.Rooms.Select(room => $"r:{room.X},{room.Y},{room.Width},{room.Height}")
                .Concat(layout.Tiles.Select(tile => $"t:{tile.Position.X},{tile.Position.Y},{tile.State}"))
                .Concat(layout.Enemies.Select(enemy => $"e:{enemy.EnemyId},{enemy.Position.X},{enemy.Position.Y}")));
    }

    private static string GridSignature(TacticalGrid grid)
    {
        return string.Join(
            ',',
            Enumerable.Range(0, grid.Height)
                .SelectMany(y => Enumerable.Range(0, grid.Width).Select(x => grid.GetTile(new GridPos(x, y))))
                .Select(state => (int)state));
    }
}
