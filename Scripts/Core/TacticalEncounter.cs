using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;
using TheThingImDoing.Spells;
using TheThingImDoing.World;

namespace TheThingImDoing.Core;

public sealed class TacticalEncounter
{
    private readonly Dictionary<int, EncounterActor> _actorsById = new();
    private readonly HashSet<ActorMark> _actorMarks = new();
    private readonly HashSet<TileMark> _tileMarks = new();
    private int _nextActorId = 1;

    public TacticalEncounter(int width, int height, GridPos playerStart)
    {
        Grid = new TacticalGrid(width, height);
        Turns = new TurnSystem();
        FloorRules = FloorRuleSet.BrittleStone();
        Player = AddActor(Faction.Player, playerStart, health: 5);
    }

    private TacticalEncounter(
        TacticalGrid grid,
        TurnSystem turns,
        FloorRuleSet floorRules,
        Dictionary<int, EncounterActor> actorsById,
        HashSet<ActorMark> actorMarks,
        HashSet<TileMark> tileMarks,
        int nextActorId,
        int playerActorId)
    {
        Grid = grid;
        Turns = turns;
        FloorRules = floorRules;
        _actorsById = actorsById;
        _actorMarks = actorMarks;
        _tileMarks = tileMarks;
        _nextActorId = nextActorId;
        Player = _actorsById[playerActorId];
    }

    public TacticalGrid Grid { get; }
    public TurnSystem Turns { get; }
    public FloorRuleSet FloorRules { get; }
    public EncounterActor Player { get; }

    public IReadOnlyCollection<EncounterActor> Actors => _actorsById.Values;

    public IEnumerable<EncounterActor> Enemies
    {
        get
        {
            return _actorsById.Values.Where(actor => actor.Faction == Faction.Enemy && actor.IsAlive);
        }
    }

    public IEnumerable<TileMark> TileMarks => _tileMarks;

    public GameResult Result
    {
        get
        {
            if (!Player.IsAlive)
            {
                return GameResult.PlayerLost;
            }

            return Enemies.Any() ? GameResult.InProgress : GameResult.PlayerWon;
        }
    }

    public EncounterActor AddDummyEnemy(GridPos position, int health = 2)
    {
        return AddEnemy("enemy.glass_hound", position, health);
    }

    public EncounterActor AddEnemy(string enemyId, GridPos position)
    {
        return AddEnemy(enemyId, position, EnemyConfigCatalog.Get(enemyId).MaxHealth);
    }

    public EncounterActor AddEnemy(string enemyId, GridPos position, int health)
    {
        return AddActor(Faction.Enemy, position, health, enemyId);
    }

    public EncounterActor? GetActor(int actorId)
    {
        return _actorsById.TryGetValue(actorId, out EncounterActor? actor) ? actor : null;
    }

    public EncounterActor? GetActorAt(GridPos position)
    {
        int? actorId = Grid.GetActorAt(position);
        return actorId.HasValue ? GetActor(actorId.Value) : null;
    }

    public EncounterActor? GetNearestEnemy(EncounterActor caster)
    {
        return Enemies
            .OrderBy(enemy => enemy.Position.ManhattanDistanceTo(caster.Position))
            .ThenBy(enemy => enemy.Id)
            .FirstOrDefault();
    }

    public string GetEnemyDisplayName(EncounterActor enemy)
    {
        return enemy.EnemyId != null && EnemyConfigCatalog.TryGet(enemy.EnemyId, out EnemyConfig? config)
            ? config.DisplayName
            : GameStrings.Get("enemies.generic.name");
    }

    public string GetEnemySigil(EncounterActor enemy)
    {
        return enemy.EnemyId != null && EnemyConfigCatalog.TryGet(enemy.EnemyId, out EnemyConfig? config)
            ? config.Sigil
            : GameStrings.Get("enemies.generic.sigil");
    }

    public string GetEnemyIntent(EncounterActor enemy)
    {
        if (enemy.Faction != Faction.Enemy)
        {
            return "";
        }

        if (enemy.EnemyId == null || !EnemyConfigCatalog.TryGet(enemy.EnemyId, out EnemyConfig? config))
        {
            return GameStrings.Get("enemies.generic.intent");
        }

        foreach (EnemyIntentRule rule in config.IntentRules)
        {
            if (DoesIntentRulePass(enemy, rule))
            {
                return GameStrings.Get(rule.IntentKey);
            }
        }

        return config.DefaultIntent;
    }

    public bool HasActorMark(int targetActorId, int ownerActorId)
    {
        return _actorMarks.Contains(new ActorMark(targetActorId, ownerActorId));
    }

    public bool HasTileMark(GridPos position, int ownerActorId)
    {
        return _tileMarks.Contains(new TileMark(position, ownerActorId));
    }

    public void AddActorMark(int targetActorId, int ownerActorId)
    {
        _actorMarks.Add(new ActorMark(targetActorId, ownerActorId));
    }

    public void AddTileMark(GridPos position, int ownerActorId)
    {
        if (Grid.IsInside(position))
        {
            _tileMarks.Add(new TileMark(position, ownerActorId));
        }
    }

    public void RemoveTileMark(GridPos position, int ownerActorId)
    {
        _tileMarks.Remove(new TileMark(position, ownerActorId));
    }

    public WorkingResult PreviewWorking(Working working, GridPos selectedTarget)
    {
        var machine = new WorkingMachine();
        TacticalEncounter previewEncounter = Clone();
        return machine.Execute(
            working,
            new EncounterSpellWorld(previewEncounter),
            previewEncounter.Player.Id,
            selectedTarget);
    }

    public WorkingResult TryCastWorking(Working working, GridPos selectedTarget)
    {
        if (Turns.Phase != TurnPhase.PlayerTurn || Result != GameResult.InProgress)
        {
            var trace = new OmenTrace();
            trace.Add("The working could not be cast right now.");
            return WorkingResult.Failed(trace, "Not player turn.");
        }

        var machine = new WorkingMachine();
        WorkingResult result = machine.Execute(
            working,
            new EncounterSpellWorld(this),
            Player.Id,
            selectedTarget);

        Turns.EndPlayerTurn();
        return result;
    }

    public bool TryMovePlayer(Direction direction)
    {
        if (Turns.Phase != TurnPhase.PlayerTurn || Result != GameResult.InProgress)
        {
            return false;
        }

        if (!TryMoveActor(Player, direction))
        {
            return false;
        }

        Turns.EndPlayerTurn();
        return true;
    }

    public bool TryPlayerStepOrAttack(Direction direction)
    {
        if (Turns.Phase != TurnPhase.PlayerTurn || Result != GameResult.InProgress)
        {
            return false;
        }

        GridPos destination = Player.Position.Offset(direction);
        int? targetActorId = Grid.GetActorAt(destination);

        if (targetActorId.HasValue
            && _actorsById.TryGetValue(targetActorId.Value, out EncounterActor? target)
            && target.Faction == Faction.Enemy)
        {
            TryDamageActor(target.Id, 1);
            Turns.EndPlayerTurn();
            return true;
        }

        if (!TryMoveActor(Player, direction))
        {
            return false;
        }

        Turns.EndPlayerTurn();
        return true;
    }

    public void WaitPlayerTurn()
    {
        if (Turns.Phase == TurnPhase.PlayerTurn && Result == GameResult.InProgress)
        {
            Turns.EndPlayerTurn();
        }
    }

    public void RunEnemyTurn()
    {
        if (Turns.Phase != TurnPhase.EnemyTurn || Result != GameResult.InProgress)
        {
            return;
        }

        foreach (EncounterActor enemy in Enemies.ToArray())
        {
            TakeDummyEnemyTurn(enemy);

            if (Result != GameResult.InProgress)
            {
                break;
            }
        }

        if (Result == GameResult.InProgress)
        {
            Turns.EndEnemyTurn();
        }
    }

    public bool TryDamageActor(int actorId, int amount)
    {
        if (!_actorsById.TryGetValue(actorId, out EncounterActor? actor) || !actor.IsAlive)
        {
            return false;
        }

        actor.ApplyDamage(amount);

        if (!actor.IsAlive)
        {
            Grid.RemoveActor(actor.Id);
        }

        return true;
    }

    public bool CanRaiseStone(GridPos position)
    {
        return Grid.IsInside(position)
            && Grid.GetTile(position) == TileState.Floor
            && !Grid.IsOccupied(position);
    }

    public bool TryRaiseStone(GridPos position)
    {
        if (!CanRaiseStone(position))
        {
            return false;
        }

        Grid.SetTile(position, TileState.RaisedStone);
        return true;
    }

    public bool TryPushActor(int actorId, Direction direction, int distance)
    {
        if (!_actorsById.TryGetValue(actorId, out EncounterActor? actor) || !actor.IsAlive)
        {
            return false;
        }

        bool moved = false;

        for (int i = 0; i < distance; i++)
        {
            GridPos destination = actor.Position.Offset(direction);

            if (Grid.IsEmpty(destination))
            {
                TryMoveActor(actor, direction);
                moved = true;
                continue;
            }

            if (TryRunPushCollisionRule(actor, destination, direction))
            {
                return true;
            }

            if (Grid.IsBlocked(destination) || Grid.IsOccupied(destination))
            {
                actor.ApplyDamage(1);

                if (actor.IsAlive)
                {
                    return moved;
                }

                Grid.RemoveActor(actor.Id);
                return true;
            }
        }

        return moved;
    }

    public TacticalEncounter Clone()
    {
        var actorClones = _actorsById.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone());

        return new TacticalEncounter(
            Grid.Clone(),
            Turns.Clone(),
            FloorRules,
            actorClones,
            _actorMarks.ToHashSet(),
            _tileMarks.ToHashSet(),
            _nextActorId,
            Player.Id);
    }

    private bool DoesIntentRulePass(EncounterActor enemy, EnemyIntentRule rule)
    {
        return rule.When switch
        {
            "adjacent_to_player" => enemy.Position.ManhattanDistanceTo(Player.Position) == 1,
            "adjacent_to_blocking" => IsAdjacentToBlockingTile(enemy.Position),
            "brain_counter_at_least" => enemy.BrainCounter >= (rule.Amount ?? 1),
            _ => false
        };
    }

    private bool TryRunPushCollisionRule(EncounterActor actor, GridPos destination, Direction direction)
    {
        if (string.IsNullOrWhiteSpace(FloorRules.PushCollisionBehaviorId))
        {
            return false;
        }

        var trace = new OmenTrace();
        BehaviorExecutionResult result = new BehaviorMachine().Execute(
            FloorRules.PushCollisionBehaviorId,
            new BehaviorExecutionContext
            {
                Encounter = this,
                EventActor = actor,
                EventTile = destination,
                EventDirection = direction,
                Trace = trace
            });

        return result.ChangedWorld;
    }

    private EncounterActor AddActor(
        Faction faction,
        GridPos position,
        int health,
        string? enemyId = null)
    {
        int actorId = _nextActorId++;
        var actor = new EncounterActor(actorId, faction, position, health, enemyId);

        if (!Grid.TryAddActor(actorId, position))
        {
            throw new InvalidOperationException($"Could not place actor {actorId} at {position}.");
        }

        _actorsById.Add(actorId, actor);
        return actor;
    }

    public bool TryMoveActor(EncounterActor actor, Direction direction)
    {
        GridPos destination = actor.Position.Offset(direction);

        if (!Grid.TryMoveActor(actor.Id, destination))
        {
            return false;
        }

        actor.Position = destination;
        return true;
    }

    private void TakeDummyEnemyTurn(EncounterActor enemy)
    {
        if (enemy.EnemyId == null || !EnemyConfigCatalog.TryGet(enemy.EnemyId, out EnemyConfig? config))
        {
            return;
        }

        new BehaviorMachine().Execute(
            config.BehaviorId,
            new BehaviorExecutionContext
            {
                Encounter = this,
                Enemy = enemy,
                Trace = new OmenTrace()
            });
    }

    public bool IsAdjacentToBlockingTile(GridPos position)
    {
        return GetAdjacentPositions(position).Any(adjacent => Grid.IsInside(adjacent) && Grid.IsBlocked(adjacent));
    }

    public static IEnumerable<GridPos> GetAdjacentPositions(GridPos position)
    {
        yield return position.Offset(Direction.North);
        yield return position.Offset(Direction.East);
        yield return position.Offset(Direction.South);
        yield return position.Offset(Direction.West);
    }
}

public sealed class EncounterActor
{
    public EncounterActor(int id, Faction faction, GridPos position, int health, string? enemyId = null)
    {
        Id = id;
        Faction = faction;
        Position = position;
        MaxHealth = health;
        Health = health;
        EnemyId = enemyId;
    }

    public int Id { get; }
    public Faction Faction { get; }
    public string? EnemyId { get; }
    public GridPos Position { get; internal set; }
    public int MaxHealth { get; }
    public int Health { get; private set; }
    public int BrainCounter { get; internal set; }
    public bool IsAlive => Health > 0;

    public void ApplyDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
    }

    public void Heal(int amount)
    {
        Health = Math.Min(MaxHealth, Health + Math.Max(0, amount));
    }

    public EncounterActor Clone()
    {
        return new EncounterActor(Id, Faction, Position, MaxHealth, EnemyId)
        {
            Health = Health,
            BrainCounter = BrainCounter
        };
    }
}

public readonly record struct ActorMark(int TargetActorId, int OwnerActorId);

public readonly record struct TileMark(GridPos Position, int OwnerActorId);
