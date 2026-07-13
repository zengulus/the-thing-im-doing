using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;
using TheThingImDoing.Progression;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class GeneratedRunPlayabilityTests
{
    private const int MaximumTurnsPerEncounter = 600;
    private static readonly Direction[] DirectionOrder =
    [
        Direction.North,
        Direction.East,
        Direction.South,
        Direction.West
    ];

    private static readonly HashSet<string> StartingClauses = new(StringComparer.Ordinal)
    {
        "clause.aim_at_target",
        "clause.aim_at_nearest_foe",
        "clause.if_marked",
        "clause.if_occupied",
        "clause.if_clear",
        "clause.mark_them",
        "clause.damage_them",
        "clause.raise_stone",
        "clause.push_them"
    };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4242)]
    [InlineData(987654321)]
    [InlineData(1732050807)]
    [InlineData(2147483646)]
    public void GeneratedRun_SensibleLegalPolicyCanClearAllFiveSeededEncounters(int runSeed)
    {
        var session = new GameRunSession("run.ashen_archive", seed: runSeed);
        var playerState = new RunPlayerState(maxHealth: 5, StartingClauses);
        var encounteredSeeds = new HashSet<int>();
        var completedEncounterIds = new List<string>();

        while (!session.IsComplete)
        {
            EncounterDefinition definition = session.CurrentEncounter!;
            int seed = session.CurrentEncounterSeed!.Value;
            encounteredSeeds.Add(seed);
            TacticalEncounter encounter = TacticalEncounterFactory.Create(
                definition,
                playerState.CurrentHealth,
                playerState.MaxHealth,
                seed);
            Working pressureWorking = CreatePressureWorking(
                playerState.UnlockedClauseIds.Contains("clause.poison_them"));

            Assert.Empty(WorkingValidator.Validate(pressureWorking));
            Assert.All(
                pressureWorking.Nodes,
                node => Assert.Contains(node.ClauseId, playerState.UnlockedClauseIds));

            int turns = RunPolicy(encounter, pressureWorking, out string recentActions);

            Assert.True(
                encounter.Result == GameResult.PlayerWon,
                $"{definition.Id} seed {seed} ended as {encounter.Result} after {turns} legal turns; " +
                $"player {encounter.Player.Position} health {encounter.Player.Health}, enemies remaining " +
                $"{string.Join(", ", encounter.Enemies.Select(enemy =>
                    $"{enemy.EnemyId}:{enemy.Health}@{enemy.Position}/visible=" +
                    CanPerceive(encounter, encounter.Player.Position, enemy.Position)))}. " +
                $"Reachable empty tiles: {CountReachableTiles(encounter)}; adjacent tiles: " +
                $"{string.Join(", ", DirectionOrder.Select(direction =>
                    $"{direction}={encounter.Grid.GetTile(encounter.Player.Position.Offset(direction))}/" +
                    $"occupied={encounter.Grid.IsOccupied(encounter.Player.Position.Offset(direction))}"))}. " +
                $"Recent actions: {recentActions}");
            Assert.InRange(turns, 1, MaximumTurnsPerEncounter);

            completedEncounterIds.Add(definition.Id);
            playerState.CaptureEncounterResult(encounter);
            Assert.True(session.TryAdvance(encounter.Result));

            if (!session.IsComplete)
            {
                IReadOnlyList<RewardDefinition> offer = RewardOfferPolicy.BuildOffer(
                    RewardDefinitionCatalog.All,
                    playerState,
                    completedEncounterIds.Count,
                    definition.RewardAmount);
                bool hasPressureUnlock = playerState.UnlockedClauseIds.Contains("clause.poison_them");
                string desiredRewardId = !hasPressureUnlock
                    ? completedEncounterIds.Count == 1
                        && playerState.CurrentHealth <= playerState.MaxHealth - 2
                            ? RewardOfferPolicy.MendingRewardId
                            : RewardOfferPolicy.FirstVictoryUnlockId
                    : playerState.CurrentHealth <= playerState.MaxHealth - 2
                        ? RewardOfferPolicy.MendingRewardId
                        : RewardOfferPolicy.DeeperVesselRewardId;
                RewardDefinition reward = Assert.Single(
                    offer,
                    candidate => candidate.Id == desiredRewardId);
                Assert.True(playerState.ApplyReward(reward));
            }
        }

        Assert.Equal(5, completedEncounterIds.Count);
        Assert.Equal(5, encounteredSeeds.Count);
        Assert.Equal(RunDefinitionCatalog.Get("run.ashen_archive").EncounterIds, completedEncounterIds);
        Assert.True(playerState.CurrentHealth > 0);
    }

    private static int RunPolicy(
        TacticalEncounter encounter,
        Working pressureWorking,
        out string recentActions)
    {
        int turn = 0;
        var actionLog = new Queue<string>();
        int? pursuedTargetId = null;

        while (encounter.Result == GameResult.InProgress && turn < MaximumTurnsPerEncounter)
        {
            turn++;
            Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);

            Direction? evasion = FindDetonationEvasion(encounter);

            if (evasion.HasValue)
            {
                Assert.True(encounter.TryMovePlayer(evasion.Value));
                RecordAction($"{turn}:evade {evasion}@{encounter.Player.Position}");
            }
            else
            {
                EncounterActor? target = pursuedTargetId.HasValue
                    ? encounter.GetActor(pursuedTargetId.Value)
                    : null;

                if (target == null || !target.IsAlive || target.Faction != Faction.Enemy)
                {
                    target = SelectNextTarget(encounter);
                    pursuedTargetId = target?.Id;
                }

                bool pursuedTargetVisible = target != null
                    && CanPerceive(encounter, encounter.Player.Position, target.Position);

                EncounterActor? urgentVisibleTarget = encounter.Enemies
                    .Where(enemy => CanPerceive(encounter, encounter.Player.Position, enemy.Position))
                    .Where(enemy => target == null
                        || GetPriority(enemy.EnemyId) < GetPriority(target.EnemyId)
                        || (!pursuedTargetVisible
                            && enemy.Position.ManhattanDistanceTo(encounter.Player.Position) == 1))
                    .OrderBy(enemy => GetPriority(enemy.EnemyId))
                    .ThenBy(enemy => enemy.Position.ManhattanDistanceTo(encounter.Player.Position))
                    .ThenBy(enemy => enemy.Id)
                    .FirstOrDefault();

                if (urgentVisibleTarget != null)
                {
                    target = urgentVisibleTarget;
                    pursuedTargetId = target.Id;
                }

                if (target != null
                    && CanPerceive(encounter, encounter.Player.Position, target.Position))
                {
                    WorkingResult cast = encounter.TryCastWorking(pressureWorking, target.Position);
                    Assert.True(
                        cast.Succeeded,
                        $"Legal cast at visible {target.EnemyId} failed: {cast.FailureReason}");
                    RecordAction($"{turn}:cast {target.EnemyId}:{target.Health}@{target.Position}");
                }
                else
                {
                    Direction? approach = target == null
                        ? null
                        : FindApproachDirection(encounter, target);

                    if (approach.HasValue)
                    {
                        Assert.True(encounter.TryMovePlayer(approach.Value));
                        RecordAction($"{turn}:move {approach}@{encounter.Player.Position}");
                    }
                    else
                    {
                        EncounterActor? blockingThreat = encounter.Enemies
                            .Where(enemy => CanPerceive(
                                encounter,
                                encounter.Player.Position,
                                enemy.Position))
                            .OrderBy(enemy => GetPriority(enemy.EnemyId))
                            .ThenBy(enemy => enemy.Position.ManhattanDistanceTo(encounter.Player.Position))
                            .ThenBy(enemy => enemy.Id)
                            .FirstOrDefault();

                        if (blockingThreat != null)
                        {
                            target = blockingThreat;
                            pursuedTargetId = target.Id;
                            WorkingResult cast = encounter.TryCastWorking(pressureWorking, target.Position);
                            Assert.True(cast.Succeeded, cast.FailureReason);
                            RecordAction($"{turn}:cast blocker {target.EnemyId}:{target.Health}@{target.Position}");
                        }
                        else
                        {
                            // A temporary stone can close a one-tile route. Waiting is a legal
                            // action, and temporary raised stone is guaranteed to clear.
                            encounter.WaitPlayerTurn();
                            RecordAction($"{turn}:wait@{encounter.Player.Position}");
                        }
                    }
                }
            }

            if (encounter.Turns.Phase == TurnPhase.EnemyTurn)
            {
                encounter.RunEnemyTurn();
            }
        }

        recentActions = string.Join(" | ", actionLog);
        return turn;

        void RecordAction(string action)
        {
            actionLog.Enqueue(action);

            while (actionLog.Count > 20)
            {
                actionLog.Dequeue();
            }
        }
    }

    private static EncounterActor? SelectNextTarget(TacticalEncounter encounter)
    {
        return encounter.Enemies
            .OrderBy(enemy => GetPriority(enemy.EnemyId))
            .ThenBy(enemy => enemy.Health)
            .ThenBy(enemy => enemy.Position.ManhattanDistanceTo(encounter.Player.Position))
            .ThenBy(enemy => enemy.Id)
            .FirstOrDefault();
    }

    private static int GetPriority(string? enemyId)
    {
        return enemyId switch
        {
            "enemy.spore_cantor" => 0,
            "enemy.moss_chirurgeon" => 1,
            "enemy.ash_scribe" => 2,
            "enemy.obsidian_crown" => 3,
            "enemy.root_saint" => 4,
            "enemy.iron_pilgrim" => 5,
            _ => 6
        };
    }

    private static Direction? FindDetonationEvasion(TacticalEncounter encounter)
    {
        bool detonationPending = encounter.TileConditions
            .Where(condition => condition.Position == encounter.Player.Position)
            .Select(condition => encounter.GetActor(condition.OwnerActorId))
            .Where(owner => owner?.Faction == Faction.Enemy && owner.IsAlive)
            .Any(owner => owner!.EnemyId switch
            {
                "enemy.ash_scribe" => owner.Counters.Get("counter.ai.charge") >= 2,
                "enemy.obsidian_crown" => owner.Counters.Get("counter.ai.ritual") >= 2,
                _ => true
            });

        if (!detonationPending)
        {
            return null;
        }

        HashSet<GridPos> hostileMarks = encounter.TileConditions
            .Where(condition => encounter.GetActor(condition.OwnerActorId)?.Faction == Faction.Enemy)
            .Select(condition => condition.Position)
            .ToHashSet();

        return DirectionOrder
            .Where(direction => encounter.Grid.IsEmpty(encounter.Player.Position.Offset(direction)))
            .OrderBy(direction => hostileMarks.Contains(encounter.Player.Position.Offset(direction)) ? 1 : 0)
            .ThenByDescending(direction => encounter.Enemies.Min(enemy =>
                enemy.Position.ManhattanDistanceTo(encounter.Player.Position.Offset(direction))))
            .Cast<Direction?>()
            .FirstOrDefault();
    }

    private static Direction? FindApproachDirection(
        TacticalEncounter encounter,
        EncounterActor target)
    {
        GridPos start = encounter.Player.Position;
        var frontier = new Queue<GridPos>();
        var visited = new HashSet<GridPos> { start };
        var firstDirections = new Dictionary<GridPos, Direction>();
        frontier.Enqueue(start);

        while (frontier.Count > 0)
        {
            GridPos current = frontier.Dequeue();

            if (current != start && CanPerceive(encounter, current, target.Position))
            {
                return firstDirections[current];
            }

            foreach (Direction direction in DirectionOrder)
            {
                GridPos next = current.Offset(direction);

                if (!visited.Add(next)
                    || !encounter.Grid.IsInside(next)
                    || encounter.Grid.IsBlocked(next)
                    || (next != start && encounter.Grid.IsOccupied(next)))
                {
                    continue;
                }

                firstDirections[next] = current == start ? direction : firstDirections[current];
                frontier.Enqueue(next);
            }
        }

        return null;
    }

    private static bool CanPerceive(TacticalEncounter encounter, GridPos from, GridPos to)
    {
        int chebyshevDistance = Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));
        return chebyshevDistance <= TacticalEncounter.EnemyAwarenessRadius
            && encounter.Grid.HasLineOfSight(from, to);
    }

    private static int CountReachableTiles(TacticalEncounter encounter)
    {
        var frontier = new Queue<GridPos>();
        var visited = new HashSet<GridPos> { encounter.Player.Position };
        frontier.Enqueue(encounter.Player.Position);

        while (frontier.Count > 0)
        {
            GridPos current = frontier.Dequeue();

            foreach (Direction direction in DirectionOrder)
            {
                GridPos next = current.Offset(direction);

                if (!encounter.Grid.IsInside(next)
                    || encounter.Grid.IsBlocked(next)
                    || (next != encounter.Player.Position && encounter.Grid.IsOccupied(next))
                    || !visited.Add(next))
                {
                    continue;
                }

                frontier.Enqueue(next);
            }
        }

        return visited.Count;
    }

    private static Working CreatePressureWorking(bool includePoison)
    {
        var working = new Working("working.test.pressure", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.damage_them") { NextNodeId = 3 });

        if (includePoison)
        {
            working.AddNode(new WorkingNode(3, "clause.poison_them") { NextNodeId = 4 });
            working.AddNode(new WorkingNode(4, "clause.push_them"));
        }
        else
        {
            working.AddNode(new WorkingNode(3, "clause.push_them"));
        }

        return working;
    }
}
