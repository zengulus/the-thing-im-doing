using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;
using TheThingImDoing.Progression;
using TheThingImDoing.Relics;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class SandboxGameTests
{
    private const string CrownEnemyId = "enemy.obsidian_crown";

    [Fact]
    public void DefaultSandboxRun_ResolvesOneFinalCrownObjectiveAndEveryOrdinaryArchetype()
    {
        RunDefinition run = RunDefinitionCatalog.Get(SandboxStartingState.RunId);
        EncounterDefinition[] encounters = run.EncounterIds
            .Select(EncounterDefinitionCatalog.Get)
            .ToArray();
        EncounterDefinition final = Assert.Single(encounters, encounter => encounter.IsFinal);
        string[] ordinaryArchetypeIds = EnemyConfigCatalog.All
            .Where(enemy => !enemy.Tags.Contains("boss") && !enemy.Tags.Contains("capstone"))
            .Select(enemy => enemy.Id)
            .OrderBy(id => id)
            .ToArray();
        string[] resolvedOrdinaryArchetypeIds = final.Enemies
            .Where(enemy => enemy.EnemyId != CrownEnemyId)
            .Select(enemy => enemy.EnemyId)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
        EncounterEnemyPlacement boss = Assert.Single(
            final.Enemies,
            enemy => IsBoss(enemy.EnemyId));

        Assert.Equal(final.Id, run.EncounterIds[^1]);
        Assert.Equal(CrownEnemyId, final.VictoryTargetEnemyId);
        Assert.Equal(CrownEnemyId, boss.EnemyId);
        Assert.Equal(6, ordinaryArchetypeIds.Length);
        Assert.Equal(ordinaryArchetypeIds, resolvedOrdinaryArchetypeIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4242)]
    [InlineData(987654321)]
    [InlineData(1732050807)]
    [InlineData(2147483646)]
    public void GeneratedSandbox_IsDeterministicConnectedAndPlacesOneReachableCrownInObjectiveBand(int seed)
    {
        EncounterDefinition definition = GetSandboxEncounter();

        EncounterLayout first = ProceduralEncounterGenerator.Generate(definition, seed);
        EncounterLayout second = ProceduralEncounterGenerator.Generate(definition, seed);
        EncounterEnemyPlacement crown = Assert.Single(
            first.Enemies,
            enemy => IsBoss(enemy.EnemyId));
        HashSet<GridPos> blocked = first.Tiles
            .Where(tile => tile.State.IsBlocking())
            .Select(tile => tile.Position)
            .ToHashSet();
        HashSet<GridPos> walkable = AllPositions(first)
            .Where(position => !blocked.Contains(position))
            .ToHashSet();
        HashSet<GridPos> reachable = FloodFill(first.PlayerStart, walkable);
        int objectiveDistance = first.PlayerStart.ManhattanDistanceTo(crown.Position);

        Assert.Equal(first.Width, second.Width);
        Assert.Equal(first.Height, second.Height);
        Assert.Equal(first.PlayerStart, second.PlayerStart);
        Assert.Equal(first.Rooms, second.Rooms);
        Assert.Equal(first.Tiles, second.Tiles);
        Assert.Equal(first.Enemies, second.Enemies);
        Assert.Equal(CrownEnemyId, crown.EnemyId);
        Assert.Equal(walkable.Count, reachable.Count);
        Assert.All(first.Enemies, enemy => Assert.Contains(enemy.Position, reachable));
        Assert.InRange(
            objectiveDistance,
            ProceduralEncounterGenerator.ObjectiveMinimumDistance,
            ProceduralEncounterGenerator.ObjectiveMaximumDistance);
    }

    [Fact]
    public void GeneratedSandbox_ObjectiveContractHoldsAcrossBroadSeedSample()
    {
        EncounterDefinition definition = GetSandboxEncounter();

        for (int seed = 0; seed < 512; seed++)
        {
            EncounterLayout layout = ProceduralEncounterGenerator.Generate(definition, seed);
            EncounterEnemyPlacement[] crowns = layout.Enemies
                .Where(enemy => enemy.EnemyId == definition.VictoryTargetEnemyId)
                .ToArray();

            Assert.True(
                crowns.Length == 1,
                $"Seed {seed} generated {crowns.Length} victory targets instead of exactly one.");

            int distance = layout.PlayerStart.ManhattanDistanceTo(crowns[0].Position);
            Assert.True(
                distance >= ProceduralEncounterGenerator.ObjectiveMinimumDistance
                    && distance <= ProceduralEncounterGenerator.ObjectiveMaximumDistance,
                $"Seed {seed} placed its victory target at distance {distance}, outside the " +
                $"{ProceduralEncounterGenerator.ObjectiveMinimumDistance}-" +
                $"{ProceduralEncounterGenerator.ObjectiveMaximumDistance} objective band.");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4242)]
    [InlineData(987654321)]
    [InlineData(1732050807)]
    [InlineData(2147483646)]
    public void SandboxStartingLoadout_LegalWorkingPolicyReachesAndDefeatsCrown(int runSeed)
    {
        var session = new GameRunSession(SandboxStartingState.RunId, runSeed);
        RunPlayerState playerState = SandboxStartingState.Create();
        EncounterDefinition definition = Assert.IsType<EncounterDefinition>(session.CurrentEncounter);
        int encounterSeed = Assert.IsType<int>(session.CurrentEncounterSeed);
        TacticalEncounter encounter = TacticalEncounterFactory.Create(
            definition,
            playerState.CurrentHealth,
            playerState.MaxHealth,
            encounterSeed);

        foreach (string relicId in playerState.RelicIds)
        {
            encounter.AddRelic(relicId);
        }

        EncounterActor crown = Assert.IsType<EncounterActor>(encounter.VictoryTarget);
        GridPos startingPosition = encounter.Player.Position;
        Working pressureWorking = CreateSetupPayoffWorking();
        Working defensiveWorking = CreateDefensiveWorking();

        Assert.True(crown.IsAlive);
        Assert.True(
            crown.Position.ManhattanDistanceTo(startingPosition)
                > TacticalEncounter.EnemyAwarenessRadius);
        Assert.Empty(WorkingValidator.Validate(pressureWorking));
        Assert.Empty(WorkingValidator.Validate(defensiveWorking));
        Assert.All(
            pressureWorking.Nodes.Concat(defensiveWorking.Nodes),
            node => Assert.Contains(node.ClauseId, playerState.UnlockedClauseIds));

        int turns = GeneratedRunPlayabilityTests.RunPolicy(
            encounter,
            pressureWorking,
            out string recentActions,
            out int successfulWorkingCasts,
            out int successfulDefensiveCasts,
            prioritizeVictoryTarget: true,
            defensiveWorking: defensiveWorking);

        Assert.True(
            encounter.Result == GameResult.PlayerWon,
            $"Sandbox run seed {runSeed} / encounter seed {encounterSeed} ended as " +
            $"{encounter.Result} after {turns} legal turns; player health " +
            $"{encounter.Player.Health}; Crown health {crown.Health}. Recent actions: {recentActions}");
        Assert.False(crown.IsAlive);
        Assert.True(successfulWorkingCasts > 0);
        Assert.True(successfulDefensiveCasts > 0);
        Assert.InRange(turns, 1, GeneratedRunPlayabilityTests.MaximumTurnsPerEncounter);
        Assert.True(session.TryAdvance(encounter.Result));
        Assert.True(session.IsComplete);
    }

    [Fact]
    public void KillingOrdinaryEnemies_DoesNotWinWhileCrownLives()
    {
        TacticalEncounter encounter = CreateSandboxEncounter();
        EncounterActor crown = Assert.IsType<EncounterActor>(encounter.VictoryTarget);
        EncounterActor[] ordinaryEnemies = encounter.Enemies
            .Where(enemy => enemy.Id != crown.Id)
            .ToArray();

        Assert.NotEmpty(ordinaryEnemies);

        foreach (EncounterActor enemy in ordinaryEnemies)
        {
            Assert.True(encounter.TryDamageActor(enemy.Id, enemy.MaxHealth));
        }

        Assert.True(crown.IsAlive);
        Assert.Equal(GameResult.InProgress, encounter.Result);
    }

    [Fact]
    public void KillingCrown_WinsWhileOrdinarySupportsRemainAlive()
    {
        TacticalEncounter encounter = CreateSandboxEncounter();
        EncounterActor crown = Assert.IsType<EncounterActor>(encounter.VictoryTarget);
        EncounterActor[] supports = encounter.Enemies
            .Where(enemy => enemy.Id != crown.Id)
            .ToArray();

        Assert.NotEmpty(supports);
        Assert.True(encounter.TryDamageActor(crown.Id, crown.MaxHealth));
        Assert.Equal(GameResult.PlayerWon, encounter.Result);
        Assert.All(supports, support => Assert.True(support.IsAlive));
    }

    [Fact]
    public void CrownKilledByTurnStartEffect_EndsEnemyTurnBeforeLethalSupportActs()
    {
        var encounter = new TacticalEncounter(
            width: 6,
            height: 4,
            playerStart: new GridPos(1, 1),
            activeFloorRuleId: "rule.brittle_stone",
            playerHealth: 1,
            playerMaxHealth: 1,
            victoryTargetEnemyId: CrownEnemyId);
        EncounterActor crown = encounter.AddEnemy(CrownEnemyId, new GridPos(4, 1));
        EncounterActor support = encounter.AddEnemy("enemy.glass_hound", new GridPos(2, 1));
        crown.ApplyDamage(crown.MaxHealth - 1);
        encounter.AttachEffectToActor(
            crown.Id,
            "effect.poison",
            encounter.Player.Id,
            stacks: 1);

        encounter.WaitPlayerTurn();
        encounter.RunEnemyTurn();

        Assert.False(crown.IsAlive);
        Assert.True(support.IsAlive);
        Assert.Equal(1, encounter.Player.Health);
        Assert.Equal(GameResult.PlayerWon, encounter.Result);
    }

    [Fact]
    public void Clone_PreservesObjectiveIdentityAndIndependentResultSemantics()
    {
        TacticalEncounter original = CreateSandboxEncounter();
        TacticalEncounter clone = original.Clone();
        EncounterActor originalCrown = Assert.IsType<EncounterActor>(original.VictoryTarget);
        EncounterActor clonedCrown = Assert.IsType<EncounterActor>(clone.VictoryTarget);

        Assert.Equal(original.VictoryTargetEnemyId, clone.VictoryTargetEnemyId);
        Assert.Equal(originalCrown.Id, clonedCrown.Id);
        Assert.Equal(originalCrown.EnemyId, clonedCrown.EnemyId);
        Assert.NotSame(originalCrown, clonedCrown);
        Assert.Equal(GameResult.InProgress, original.Result);
        Assert.Equal(GameResult.InProgress, clone.Result);

        Assert.True(clone.TryDamageActor(clonedCrown.Id, clonedCrown.MaxHealth));

        Assert.Equal(GameResult.InProgress, original.Result);
        Assert.True(originalCrown.IsAlive);
        Assert.Equal(GameResult.PlayerWon, clone.Result);

        TacticalEncounter completedClone = clone.Clone();
        EncounterActor completedCloneCrown = Assert.IsType<EncounterActor>(completedClone.VictoryTarget);

        Assert.Equal(clonedCrown.Id, completedCloneCrown.Id);
        Assert.False(completedCloneCrown.IsAlive);
        Assert.Equal(GameResult.PlayerWon, completedClone.Result);
    }

    [Fact]
    public void RuntimeDuplicateObjective_FailsClosedInsteadOfChoosingOneArbitrarily()
    {
        var encounter = new TacticalEncounter(
            width: 6,
            height: 4,
            playerStart: new GridPos(1, 1),
            activeFloorRuleId: "rule.brittle_stone",
            playerHealth: 3,
            playerMaxHealth: 3,
            victoryTargetEnemyId: CrownEnemyId);
        EncounterActor first = encounter.AddEnemy(CrownEnemyId, new GridPos(3, 1));
        encounter.AddEnemy(CrownEnemyId, new GridPos(4, 1));

        Assert.Null(encounter.VictoryTarget);
        Assert.True(encounter.TryDamageActor(first.Id, first.MaxHealth));
        Assert.Equal(GameResult.InProgress, encounter.Result);
    }

    [Fact]
    public void SandboxStartingState_GrantsEveryClauseAndEveryRegisteredRelic()
    {
        RunPlayerState state = SandboxStartingState.Create();
        string[] expectedClauseIds = ClauseDefinitionCatalog.All
            .Select(clause => clause.Id)
            .OrderBy(id => id)
            .ToArray();
        string[] expectedRelicIds = RelicDefinitionCatalog.All
            .Select(relic => relic.Id)
            .OrderBy(id => id)
            .ToArray();

        Assert.NotEmpty(expectedClauseIds);
        Assert.NotEmpty(expectedRelicIds);
        Assert.Equal(expectedClauseIds, state.UnlockedClauseIds.OrderBy(id => id));
        Assert.Equal(expectedRelicIds, state.RelicIds.OrderBy(id => id));
    }

    private static EncounterDefinition GetSandboxEncounter()
    {
        RunDefinition run = RunDefinitionCatalog.Get(SandboxStartingState.RunId);
        string encounterId = Assert.Single(run.EncounterIds);
        return EncounterDefinitionCatalog.Get(encounterId);
    }

    private static TacticalEncounter CreateSandboxEncounter()
    {
        return TacticalEncounterFactory.Create(
            GetSandboxEncounter(),
            playerHealth: SandboxStartingState.MaxHealth,
            playerMaxHealth: SandboxStartingState.MaxHealth,
            proceduralSeed: 4242);
    }

    private static Working CreateSetupPayoffWorking()
    {
        var working = new Working(
            "working.test.sandbox_setup_payoff",
            "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.if_marked")
        {
            TrueNodeId = 3,
            FalseNodeId = 4
        });
        working.AddNode(new WorkingNode(3, "clause.damage_them") { NextNodeId = 4 });
        working.AddNode(new WorkingNode(4, "clause.mark_them") { NextNodeId = 5 });
        working.AddNode(new WorkingNode(5, "clause.push_them"));
        return working;
    }

    private static Working CreateDefensiveWorking()
    {
        var working = new Working(
            "working.test.sandbox_storm_guard",
            "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.add_lightning_charge") { NextNodeId = 3 });
        working.AddNode(new WorkingNode(3, "clause.lightning_shield") { NextNodeId = 4 });
        working.AddNode(new WorkingNode(4, "clause.add_lightning_ward"));
        return working;
    }

    private static bool IsBoss(string enemyId)
    {
        EnemyConfig config = EnemyConfigCatalog.Get(enemyId);
        return config.Tags.Contains("boss") || config.Tags.Contains("capstone");
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

    private static HashSet<GridPos> FloodFill(GridPos start, IReadOnlySet<GridPos> walkable)
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
            if (!walkable.Contains(position) || !visited.Add(position))
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
}
