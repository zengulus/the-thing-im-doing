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
    private readonly Dictionary<GridPos, CounterSet> _tileCounters = new();
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
        Dictionary<GridPos, CounterSet> tileCounters,
        int nextActorId,
        int playerActorId)
    {
        Grid = grid;
        Turns = turns;
        FloorRules = floorRules;
        _actorsById = actorsById;
        _tileCounters = tileCounters;
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
            return _actorsById.Values.Where(actor => actor.IsAlive && IsHostile(Player, actor));
        }
    }

    public IEnumerable<EncounterActor> GetActorsByRelation(EncounterActor source, string relation)
    {
        return _actorsById.Values.Where(actor => actor.IsAlive && DoesRelationMatch(source, actor, relation));
    }

    public IEnumerable<TileCondition> TileConditions
    {
        get
        {
            foreach ((GridPos position, CounterSet counters) in _tileCounters)
            {
                foreach ((string counterId, int amount) in counters.All)
                {
                    if (amount > 0 && TryGetOwnedCondition(counterId, out string conditionId, out int ownerActorId))
                    {
                        yield return new TileCondition(position, conditionId, ownerActorId);
                    }
                }
            }
        }
    }

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

    public EncounterActor? GetNearestActor(EncounterActor source, string relation)
    {
        return GetActorsByRelation(source, relation)
            .Where(actor => actor.Id != source.Id || relation == "self")
            .OrderBy(actor => actor.Position.ManhattanDistanceTo(source.Position))
            .ThenBy(actor => actor.Id)
            .FirstOrDefault();
    }

    public bool IsHostile(EncounterActor source, EncounterActor target)
    {
        return source.Faction != Faction.Neutral
            && target.Faction != Faction.Neutral
            && source.Faction != target.Faction;
    }

    public bool IsAlly(EncounterActor source, EncounterActor target)
    {
        return source.Id != target.Id
            && source.Faction != Faction.Neutral
            && source.Faction == target.Faction;
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

    public bool HasActorCondition(int targetActorId, string conditionId, int ownerActorId)
    {
        return GetActorCounter(targetActorId, GetOwnedConditionId(conditionId, ownerActorId)) > 0;
    }

    public bool HasTileCondition(GridPos position, string conditionId, int ownerActorId)
    {
        return GetTileCounter(position, GetOwnedConditionId(conditionId, ownerActorId)) > 0;
    }

    public int GetActorCounter(int actorId, string counterId)
    {
        return _actorsById.TryGetValue(actorId, out EncounterActor? actor)
            ? actor.Counters.Get(counterId)
            : 0;
    }

    public int AddActorCounter(int actorId, string counterId, int amount)
    {
        return _actorsById.TryGetValue(actorId, out EncounterActor? actor)
            ? actor.Counters.Add(counterId, amount)
            : 0;
    }

    public LingeringEffectInstance? AttachLingeringEffect(
        int targetActorId,
        string effectId,
        int ownerActorId,
        int stacks)
    {
        if (!_actorsById.TryGetValue(targetActorId, out EncounterActor? target)
            || !LingeringEffectDefinitionCatalog.TryGet(effectId, out LingeringEffectDefinition? definition))
        {
            return null;
        }

        LingeringEffectInstance instance = target.GetOrAddLingeringEffect(effectId, ownerActorId);

        if (stacks > 0 && definition.AllowsCounter("counter.stack"))
        {
            instance.Counters.Add("counter.stack", stacks);
        }

        RunLingeringBehavior(target, instance, definition.OnApplyBehaviorId);
        return instance;
    }

    public bool DetachLingeringEffect(int targetActorId, int instanceId)
    {
        return _actorsById.TryGetValue(targetActorId, out EncounterActor? target)
            && target.RemoveLingeringEffect(instanceId);
    }

    public LingeringEffectInstance? GetLingeringEffect(int targetActorId, int instanceId)
    {
        return _actorsById.TryGetValue(targetActorId, out EncounterActor? target)
            ? target.LingeringEffects.FirstOrDefault(effect => effect.InstanceId == instanceId)
            : null;
    }

    public int GetTileCounter(GridPos position, string counterId)
    {
        return _tileCounters.TryGetValue(position, out CounterSet? counters)
            ? counters.Get(counterId)
            : 0;
    }

    public int AddTileCounter(GridPos position, string counterId, int amount)
    {
        if (!Grid.IsInside(position))
        {
            return 0;
        }

        if (!_tileCounters.TryGetValue(position, out CounterSet? counters))
        {
            counters = new CounterSet();
            _tileCounters.Add(position, counters);
        }

        int next = counters.Add(counterId, amount);

        if (counters.All.Count == 0)
        {
            _tileCounters.Remove(position);
        }

        return next;
    }

    public void AddActorCondition(int targetActorId, string conditionId, int ownerActorId)
    {
        AddActorCounter(targetActorId, GetOwnedConditionId(conditionId, ownerActorId), 1);
    }

    public void AddTileCondition(GridPos position, string conditionId, int ownerActorId)
    {
        if (Grid.IsInside(position))
        {
            AddTileCounter(position, GetOwnedConditionId(conditionId, ownerActorId), 1);
        }
    }

    public void RemoveTileCondition(GridPos position, string conditionId, int ownerActorId)
    {
        AddTileCounter(position, GetOwnedConditionId(conditionId, ownerActorId), -1);
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
            && IsHostile(Player, target))
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
            RunLingeringHook(enemy, LingeringEffectHook.TurnStart);

            if (!enemy.IsAlive)
            {
                Grid.RemoveActor(enemy.Id);
                continue;
            }

            TakeDummyEnemyTurn(enemy);

            if (Result != GameResult.InProgress)
            {
                break;
            }
        }

        if (Result == GameResult.InProgress)
        {
            RunLingeringHook(Player, LingeringEffectHook.TurnStart);
        }

        if (Result == GameResult.InProgress)
        {
            Turns.EndEnemyTurn();
        }
    }

    public bool TryDamageActor(int actorId, int amount)
    {
        if (amount <= 0 || !_actorsById.TryGetValue(actorId, out EncounterActor? actor) || !actor.IsAlive)
        {
            return false;
        }

        int nextAmount = RunBeforeDamageHooks(actor, amount);

        if (nextAmount <= 0)
        {
            return false;
        }

        actor.ApplyDamage(nextAmount);

        if (!actor.IsAlive)
        {
            RunLingeringHook(actor, LingeringEffectHook.Death);
            Grid.RemoveActor(actor.Id);
        }

        return true;
    }

    public bool CanSetTileState(GridPos position, TileState state)
    {
        if (!Grid.IsInside(position))
        {
            return false;
        }

        if (state.IsBlocking() && Grid.IsOccupied(position))
        {
            return false;
        }

        return true;
    }

    public bool TrySetTileState(GridPos position, TileState state)
    {
        if (!CanSetTileState(position, state))
        {
            return false;
        }

        Grid.SetTile(position, state);
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
                TryDamageActor(actor.Id, 1);

                if (actor.IsAlive)
                {
                    return moved;
                }

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
        var tileCounterClones = _tileCounters.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone());

        return new TacticalEncounter(
            Grid.Clone(),
            Turns.Clone(),
            FloorRules,
            actorClones,
            tileCounterClones,
            _nextActorId,
            Player.Id);
    }

    private bool DoesIntentRulePass(EncounterActor enemy, EnemyIntentRule rule)
    {
        return rule.When switch
        {
            "adjacent_to_player" => enemy.Position.ManhattanDistanceTo(Player.Position) == 1,
            "adjacent_to_blocking" => IsAdjacentToBlockingTile(enemy.Position),
            "self_counter_at_least" => enemy.Counters.Get(rule.Counter) >= (rule.Amount ?? 1),
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
        GridPos origin = actor.Position;
        GridPos destination = actor.Position.Offset(direction);

        if (!Grid.TryMoveActor(actor.Id, destination))
        {
            return false;
        }

        actor.Position = destination;
        RunLingeringHook(actor, LingeringEffectHook.Move);
        RunBecameAdjacentHooks(actor, origin);
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

    private void RunLingeringHook(EncounterActor target, LingeringEffectHook hook)
    {
        foreach (LingeringEffectInstance instance in target.LingeringEffects.ToArray())
        {
            if (!LingeringEffectDefinitionCatalog.TryGet(instance.EffectId, out LingeringEffectDefinition? definition))
            {
                continue;
            }

            string behaviorId = hook switch
            {
                LingeringEffectHook.TurnStart => definition.OnTurnStartBehaviorId,
                LingeringEffectHook.Move => definition.OnMoveBehaviorId,
                LingeringEffectHook.Death => definition.OnDeathBehaviorId,
                LingeringEffectHook.ActorBecameAdjacent => definition.OnActorBecameAdjacentBehaviorId,
                LingeringEffectHook.BeforeDamage => definition.OnBeforeDamageBehaviorId,
                _ => ""
            };

            RunLingeringBehavior(target, instance, behaviorId);

            if (!target.IsAlive)
            {
                break;
            }
        }
    }

    private int RunBeforeDamageHooks(EncounterActor target, int amount)
    {
        int nextAmount = amount;

        foreach (LingeringEffectInstance instance in target.LingeringEffects.ToArray())
        {
            if (!LingeringEffectDefinitionCatalog.TryGet(instance.EffectId, out LingeringEffectDefinition? definition)
                || string.IsNullOrWhiteSpace(definition.OnBeforeDamageBehaviorId))
            {
                continue;
            }

            nextAmount = RunLingeringBehavior(
                target,
                instance,
                definition.OnBeforeDamageBehaviorId,
                eventActor: target,
                eventDamage: nextAmount).EventDamage;

            if (nextAmount <= 0)
            {
                return 0;
            }
        }

        return nextAmount;
    }

    private void RunBecameAdjacentHooks(EncounterActor movedActor, GridPos origin)
    {
        foreach (EncounterActor target in Actors.Where(actor => actor.IsAlive && actor.Id != movedActor.Id).ToArray())
        {
            bool wasAdjacent = origin.ManhattanDistanceTo(target.Position) == 1;
            bool isAdjacent = movedActor.Position.ManhattanDistanceTo(target.Position) == 1;

            if (wasAdjacent || !isAdjacent)
            {
                continue;
            }

            foreach (LingeringEffectInstance instance in target.LingeringEffects.ToArray())
            {
                if (!LingeringEffectDefinitionCatalog.TryGet(instance.EffectId, out LingeringEffectDefinition? definition))
                {
                    continue;
                }

                RunLingeringBehavior(
                    target,
                    instance,
                    definition.OnActorBecameAdjacentBehaviorId,
                    eventActor: movedActor);

                if (!movedActor.IsAlive || !target.IsAlive)
                {
                    break;
                }
            }
        }
    }

    private BehaviorExecutionContext RunLingeringBehavior(
        EncounterActor target,
        LingeringEffectInstance instance,
        string behaviorId,
        EncounterActor? eventActor = null,
        int eventDamage = 0)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return new BehaviorExecutionContext
            {
                Encounter = this,
                EventActor = eventActor ?? target,
                EventDamage = eventDamage,
                LingeringTarget = target,
                LingeringEffect = instance,
                Trace = new OmenTrace()
            };
        }

        var context = new BehaviorExecutionContext
        {
            Encounter = this,
            EventActor = eventActor ?? target,
            EventDamage = eventDamage,
            LingeringTarget = target,
            LingeringEffect = instance,
            Trace = new OmenTrace()
        };

        new BehaviorMachine().Execute(
            behaviorId,
            context);

        return context;
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

    private static string GetOwnedConditionId(string conditionId, int ownerActorId)
    {
        return $"{conditionId}.owner.{ownerActorId}";
    }

    private static bool TryGetOwnedCondition(string counterId, out string conditionId, out int ownerActorId)
    {
        const string OwnerToken = ".owner.";

        int ownerTokenIndex = counterId.LastIndexOf(OwnerToken, StringComparison.Ordinal);

        if (counterId.StartsWith("condition.")
            && ownerTokenIndex > 0
            && int.TryParse(counterId[(ownerTokenIndex + OwnerToken.Length)..], out ownerActorId))
        {
            conditionId = counterId[..ownerTokenIndex];
            return true;
        }

        conditionId = "";
        ownerActorId = 0;
        return false;
    }

    private bool DoesRelationMatch(EncounterActor source, EncounterActor target, string relation)
    {
        return relation switch
        {
            "self" => source.Id == target.Id,
            "ally" => IsAlly(source, target),
            "hostile" or "enemy" => IsHostile(source, target),
            "neutral" => target.Faction == Faction.Neutral,
            "player" => target.Faction == Faction.Player,
            "any" or "" => source.Id != target.Id,
            _ => false
        };
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
    public CounterSet Counters { get; } = new();
    public IReadOnlyList<LingeringEffectInstance> LingeringEffects => _lingeringEffects;
    public bool IsAlive => Health > 0;

    private readonly List<LingeringEffectInstance> _lingeringEffects = new();
    private int _nextLingeringEffectInstanceId = 1;

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
        var clone = new EncounterActor(Id, Faction, Position, MaxHealth, EnemyId)
        {
            Health = Health
        };

        foreach ((string counterId, int amount) in Counters.All)
        {
            clone.Counters.Add(counterId, amount);
        }

        clone._nextLingeringEffectInstanceId = _nextLingeringEffectInstanceId;

        foreach (LingeringEffectInstance effect in _lingeringEffects)
        {
            clone._lingeringEffects.Add(effect.Clone());
        }

        return clone;
    }

    public LingeringEffectInstance GetOrAddLingeringEffect(string effectId, int ownerActorId)
    {
        LingeringEffectInstance? existing = _lingeringEffects.FirstOrDefault(effect =>
            effect.EffectId == effectId && effect.OwnerActorId == ownerActorId);

        if (existing != null)
        {
            return existing;
        }

        var instance = new LingeringEffectInstance(_nextLingeringEffectInstanceId++, effectId, ownerActorId);
        _lingeringEffects.Add(instance);
        return instance;
    }

    public LingeringEffectInstance? FindLingeringEffect(string effectId)
    {
        return _lingeringEffects.FirstOrDefault(effect => effect.EffectId == effectId);
    }

    public bool RemoveLingeringEffect(int instanceId)
    {
        return _lingeringEffects.RemoveAll(effect => effect.InstanceId == instanceId) > 0;
    }
}

public readonly record struct TileCondition(GridPos Position, string ConditionId, int OwnerActorId);

public enum LingeringEffectHook
{
    TurnStart,
    Move,
    Death,
    ActorBecameAdjacent,
    BeforeDamage
}

public sealed class LingeringEffectInstance
{
    public LingeringEffectInstance(int instanceId, string effectId, int ownerActorId)
    {
        InstanceId = instanceId;
        EffectId = effectId;
        OwnerActorId = ownerActorId;
    }

    public int InstanceId { get; }
    public string EffectId { get; }
    public int OwnerActorId { get; }
    public CounterSet Counters { get; } = new();

    public LingeringEffectInstance Clone()
    {
        var clone = new LingeringEffectInstance(InstanceId, EffectId, OwnerActorId);

        foreach ((string counterId, int amount) in Counters.All)
        {
            clone.Counters.Add(counterId, amount);
        }

        return clone;
    }
}
