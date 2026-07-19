using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;
using TheThingImDoing.Relics;
using TheThingImDoing.Spells;
using TheThingImDoing.World;

namespace TheThingImDoing.Core;

public sealed class TacticalEncounter
{
    public const int EnemyAwarenessRadius = 7;
    public const int TemporaryRaisedStoneDuration = 3;

    private readonly Dictionary<int, EncounterActor> _actorsById = new();
    private readonly Dictionary<GridPos, CounterSet> _tileCounters = new();
    private readonly Dictionary<GridPos, List<EffectInstance>> _tileEffects = new();
    private readonly Dictionary<GridPos, int> _temporaryRaisedStoneRounds = new();
    private readonly List<string> _relicIds = new();
    private readonly EffectHookRecursionGuard _hookRecursionGuard = new();
    private int _nextActorId = 1;
    private int _nextTileEffectInstanceId = 1;

    public TacticalEncounter(int width, int height, GridPos playerStart)
        : this(
            width,
            height,
            playerStart,
            "rule.brittle_stone",
            playerHealth: 5,
            playerMaxHealth: 5)
    {
    }

    public TacticalEncounter(
        int width,
        int height,
        GridPos playerStart,
        string activeFloorRuleId,
        int playerHealth,
        int playerMaxHealth,
        string? victoryTargetEnemyId = null)
    {
        if (playerMaxHealth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerMaxHealth), "Player max health must be positive.");
        }

        if (playerHealth <= 0 || playerHealth > playerMaxHealth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerHealth),
                "Player health must be positive and no greater than max health.");
        }

        if (string.IsNullOrWhiteSpace(activeFloorRuleId))
        {
            throw new ArgumentException("An active floor rule id is required.", nameof(activeFloorRuleId));
        }

        Grid = new TacticalGrid(width, height);
        Turns = new TurnSystem();
        FloorRules = new FloorRuleSet(activeFloorRuleId);
        VictoryTargetEnemyId = string.IsNullOrWhiteSpace(victoryTargetEnemyId)
            ? null
            : victoryTargetEnemyId.Trim();
        Player = AddActor(Faction.Player, playerStart, health: playerMaxHealth);
        Player.ApplyDamage(playerMaxHealth - playerHealth);
    }

    private TacticalEncounter(
        TacticalGrid grid,
        TurnSystem turns,
        FloorRuleSet floorRules,
        Dictionary<int, EncounterActor> actorsById,
        Dictionary<GridPos, CounterSet> tileCounters,
        Dictionary<GridPos, List<EffectInstance>> tileEffects,
        Dictionary<GridPos, int> temporaryRaisedStoneRounds,
        List<string> relicIds,
        int nextActorId,
        int nextTileEffectInstanceId,
        int playerActorId,
        string? victoryTargetEnemyId)
    {
        Grid = grid;
        Turns = turns;
        FloorRules = floorRules;
        _actorsById = actorsById;
        _tileCounters = tileCounters;
        _tileEffects = tileEffects;
        _temporaryRaisedStoneRounds = temporaryRaisedStoneRounds;
        _relicIds = relicIds;
        _nextActorId = nextActorId;
        _nextTileEffectInstanceId = nextTileEffectInstanceId;
        VictoryTargetEnemyId = victoryTargetEnemyId;
        Player = _actorsById[playerActorId];
    }

    public TacticalGrid Grid { get; }
    public TurnSystem Turns { get; }
    public FloorRuleSet FloorRules { get; }
    public EncounterActor Player { get; }
    public string? VictoryTargetEnemyId { get; }

    public EncounterActor? VictoryTarget
    {
        get
        {
            if (VictoryTargetEnemyId == null)
            {
                return null;
            }

            EncounterActor[] candidates = _actorsById.Values
                .Where(actor =>
                    actor.EnemyId == VictoryTargetEnemyId
                    && IsHostile(Player, actor))
                .Take(2)
                .ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }
    }

    public IReadOnlyCollection<EncounterActor> Actors => _actorsById.Values;
    public IReadOnlyList<string> RelicIds => _relicIds;

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
            foreach ((GridPos position, List<EffectInstance> effects) in _tileEffects)
            {
                foreach (EffectInstance effect in effects)
                {
                    if (IsCondition(effect.EffectId))
                    {
                        yield return new TileCondition(position, effect.EffectId, effect.OwnerActorId);
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

            if (VictoryTargetEnemyId != null)
            {
                EncounterActor? victoryTarget = VictoryTarget;

                if (victoryTarget == null || victoryTarget.IsAlive)
                {
                    return GameResult.InProgress;
                }

                return GameResult.PlayerWon;
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
        return HasActorEffect(targetActorId, conditionId, ownerActorId);
    }

    public bool HasTileCondition(GridPos position, string conditionId, int ownerActorId)
    {
        return HasTileEffect(position, conditionId, ownerActorId);
    }

    public EffectInstance? FindTileEffect(
        GridPos position,
        string effectId,
        int? ownerActorId = null)
    {
        if (!_tileEffects.TryGetValue(position, out List<EffectInstance>? effects))
        {
            return null;
        }

        return effects.FirstOrDefault(effect =>
            effect.EffectId == effectId
            && (!ownerActorId.HasValue || effect.OwnerActorId == ownerActorId.Value));
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

    public EffectInstance? AttachEffectToActor(
        int targetActorId,
        string effectId,
        int ownerActorId,
        int stacks)
    {
        return AttachEffectToActorDetailed(targetActorId, effectId, ownerActorId, stacks).Effect;
    }

    private EffectAttachmentResult AttachEffectToActorDetailed(
        int targetActorId,
        string effectId,
        int ownerActorId,
        int stacks,
        OmenTrace? trace = null)
    {
        if (!_actorsById.TryGetValue(targetActorId, out EncounterActor? target)
            || !EffectDefinitionCatalog.TryGet(effectId, out EffectDefinition? definition))
        {
            return EffectAttachmentResult.NoChange;
        }

        bool created = target.FindEffect(effectId, ownerActorId) == null;
        EffectInstance instance = target.GetOrAddEffect(effectId, ownerActorId);
        int stacksAdded = AddStartingStacks(instance, definition, stacks);

        if (!created && stacksAdded == 0)
        {
            return new EffectAttachmentResult(instance, ChangedWorld: false, StacksAdded: 0);
        }

        OmenTrace effectTrace = trace ?? new OmenTrace();
        RunEffectTrigger(target, instance, EffectTriggerIds.Apply, trace: effectTrace);
        RunRuleHooks(
            RuleTriggerIds.EffectApplied,
            effectTrace,
            eventActor: target,
            eventDamage: stacksAdded);
        RunRelicHooks(
            RuleTriggerIds.EffectApplied,
            effectTrace,
            eventActor: target,
            eventDamage: stacksAdded);
        RunRelicHooks(
            EffectTriggerIds.Apply,
            effectTrace,
            eventActor: target,
            eventDamage: stacksAdded);
        return new EffectAttachmentResult(instance, ChangedWorld: true, StacksAdded: stacksAdded);
    }

    public EffectInstance? AttachEffectToTile(
        GridPos position,
        string effectId,
        int ownerActorId,
        int stacks,
        OmenTrace? trace = null)
    {
        return AttachEffectToTileDetailed(position, effectId, ownerActorId, stacks, trace).Effect;
    }

    private EffectAttachmentResult AttachEffectToTileDetailed(
        GridPos position,
        string effectId,
        int ownerActorId,
        int stacks,
        OmenTrace? trace = null)
    {
        if (!Grid.IsInside(position)
            || !EffectDefinitionCatalog.TryGet(effectId, out EffectDefinition? definition))
        {
            return EffectAttachmentResult.NoChange;
        }

        bool created = !_tileEffects.TryGetValue(position, out List<EffectInstance>? effects)
            || effects.All(effect => effect.EffectId != effectId || effect.OwnerActorId != ownerActorId);
        EffectInstance instance = GetOrAddTileEffect(position, effectId, ownerActorId);
        int stacksAdded = AddStartingStacks(instance, definition, stacks);

        if (!created && stacksAdded == 0)
        {
            return new EffectAttachmentResult(instance, ChangedWorld: false, StacksAdded: 0);
        }

        OmenTrace effectTrace = trace ?? new OmenTrace();
        RunRuleHooks(
            RuleTriggerIds.EffectApplied,
            effectTrace,
            eventTile: position,
            eventDamage: stacksAdded);
        RunRelicHooks(
            RuleTriggerIds.EffectApplied,
            effectTrace,
            eventTile: position,
            eventDamage: stacksAdded);
        RunRelicHooks(
            EffectTriggerIds.Apply,
            effectTrace,
            eventTile: position,
            eventDamage: stacksAdded);
        return new EffectAttachmentResult(instance, ChangedWorld: true, StacksAdded: stacksAdded);
    }

    public bool DetachEffectFromActor(int targetActorId, int instanceId)
    {
        return _actorsById.TryGetValue(targetActorId, out EncounterActor? target)
            && target.RemoveEffect(instanceId);
    }

    public bool DetachEffectFromTile(GridPos position, int instanceId)
    {
        if (!_tileEffects.TryGetValue(position, out List<EffectInstance>? effects))
        {
            return false;
        }

        bool removed = effects.RemoveAll(effect => effect.InstanceId == instanceId) > 0;

        if (effects.Count == 0)
        {
            _tileEffects.Remove(position);
        }

        return removed;
    }

    public EffectInstance? GetEffect(int targetActorId, int instanceId)
    {
        return _actorsById.TryGetValue(targetActorId, out EncounterActor? target)
            ? target.Effects.FirstOrDefault(effect => effect.InstanceId == instanceId)
            : null;
    }

    public bool HasActorEffect(int targetActorId, string effectId, int ownerActorId)
    {
        return _actorsById.TryGetValue(targetActorId, out EncounterActor? target)
            && target.FindEffect(effectId, ownerActorId) != null;
    }

    public bool HasTileEffect(GridPos position, string effectId, int ownerActorId)
    {
        return _tileEffects.TryGetValue(position, out List<EffectInstance>? effects)
            && effects.Any(effect => effect.EffectId == effectId && effect.OwnerActorId == ownerActorId);
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
        AttachEffectToActor(targetActorId, conditionId, ownerActorId, stacks: 0);
    }

    public void AddTileCondition(GridPos position, string conditionId, int ownerActorId)
    {
        if (Grid.IsInside(position))
        {
            AttachEffectToTile(position, conditionId, ownerActorId, stacks: 0);
        }
    }

    public void RemoveTileCondition(GridPos position, string conditionId, int ownerActorId)
    {
        if (!_tileEffects.TryGetValue(position, out List<EffectInstance>? effects))
        {
            return;
        }

        effects.RemoveAll(effect => effect.EffectId == conditionId && effect.OwnerActorId == ownerActorId);

        if (effects.Count == 0)
        {
            _tileEffects.Remove(position);
        }
    }

    public WorkingResult PreviewWorking(Working working, GridPos selectedTarget)
    {
        return PreviewWorkingDetailed(working, selectedTarget).Result;
    }

    public WorkingPreview PreviewWorkingDetailed(Working working, GridPos selectedTarget)
    {
        var machine = new WorkingMachine();
        TacticalEncounter previewEncounter = Clone();
        WorkingResult result = machine.Execute(
            working,
            new EncounterSpellWorld(previewEncounter),
            previewEncounter.Player.Id,
            selectedTarget);

        if (!result.Succeeded)
        {
            return new WorkingPreview(RollBackFailedWorking(result), Clone());
        }

        if (!result.ChangedWorld
            || WorkingValidator.Validate(working).Count > 0)
        {
            return new WorkingPreview(result, previewEncounter);
        }

        result = result.WithHookChanges(previewEncounter.RunAfterSpellResolvedHooks(result.Trace));
        return new WorkingPreview(result, previewEncounter);
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
        TacticalEncounter preflightEncounter = Clone();
        WorkingResult preflight = machine.Execute(
            working,
            new EncounterSpellWorld(preflightEncounter),
            preflightEncounter.Player.Id,
            selectedTarget);

        if (!preflight.Succeeded)
        {
            return RollBackFailedWorking(preflight);
        }

        WorkingResult result = machine.Execute(
            working,
            new EncounterSpellWorld(this),
            Player.Id,
            selectedTarget);

        if (result.ChangedWorld)
        {
            result = result.WithHookChanges(RunAfterSpellResolvedHooks(result.Trace));
        }

        Turns.EndPlayerTurn();
        return result;
    }

    private static WorkingResult RollBackFailedWorking(WorkingResult result)
    {
        result.Trace.Add("The working failed transactionally; any prior effects were unwound.");
        return WorkingResult.Failed(
            result.Trace,
            result.FailureReason ?? "Working failed.");
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

        UpdateEnemyAwareness();

        foreach (EncounterActor enemy in Enemies.ToArray())
        {
            RunEffectTrigger(enemy, EffectTriggerIds.TurnStart);
            RunRelicHooks(EffectTriggerIds.TurnStart, new OmenTrace(), eventActor: enemy);

            if (!enemy.IsAlive)
            {
                Grid.RemoveActor(enemy.Id);
            }

            if (Result != GameResult.InProgress)
            {
                break;
            }

            if (!enemy.IsAlive)
            {
                continue;
            }

            if (IsEnemyEngaged(enemy))
            {
                TakeDummyEnemyTurn(enemy);
            }

            if (Result != GameResult.InProgress)
            {
                break;
            }
        }

        if (Result == GameResult.InProgress)
        {
            RunEffectTrigger(Player, EffectTriggerIds.TurnStart);
            RunRelicHooks(EffectTriggerIds.TurnStart, new OmenTrace(), eventActor: Player);
            TickTemporaryRaisedStone();
        }

        if (Result == GameResult.InProgress)
        {
            Turns.EndEnemyTurn();
        }
    }

    public bool TryDamageActor(int actorId, int amount)
    {
        return ResolveDamageActor(actorId, amount, trace: null).AppliedAmount > 0;
    }

    private DamageResolution ResolveDamageActor(int actorId, int amount, OmenTrace? trace)
    {
        if (amount <= 0 || !_actorsById.TryGetValue(actorId, out EncounterActor? actor) || !actor.IsAlive)
        {
            return DamageResolution.NoChange;
        }

        BeforeDamageResult beforeDamage = RunBeforeDamageHooks(actor, amount, trace);

        if (beforeDamage.Amount <= 0)
        {
            return new DamageResolution(beforeDamage.ChangedWorld, 0, WasPrevented: true);
        }

        int healthBeforeDamage = actor.Health;
        actor.ApplyDamage(beforeDamage.Amount);
        int appliedAmount = healthBeforeDamage - actor.Health;

        if (actor.Faction == Faction.Enemy)
        {
            actor.IsAlerted = true;
        }

        RunRuleHooks(
            RuleTriggerIds.AfterDamage,
            trace ?? new OmenTrace(),
            eventActor: actor,
            eventDamage: appliedAmount);
        RunRelicHooks(
            RuleTriggerIds.AfterDamage,
            trace ?? new OmenTrace(),
            eventActor: actor,
            eventDamage: appliedAmount);

        if (!actor.IsAlive)
        {
            RunEffectTrigger(actor, EffectTriggerIds.Death);
            RemoveTileEffectsOwnedBy(actor.Id);
            Grid.RemoveActor(actor.Id);
        }

        return new DamageResolution(
            beforeDamage.ChangedWorld || appliedAmount > 0,
            appliedAmount,
            WasPrevented: false);
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

        if (state == TileState.RaisedStone)
        {
            _temporaryRaisedStoneRounds[position] = TemporaryRaisedStoneDuration;
        }
        else
        {
            _temporaryRaisedStoneRounds.Remove(position);
        }

        RunRuleHooks(RuleTriggerIds.TileStateChanged, new OmenTrace(), eventTile: position);
        RunRelicHooks(RuleTriggerIds.TileStateChanged, new OmenTrace(), eventTile: position);
        return true;
    }

    public bool TryPushActor(int actorId, Direction direction, int distance, OmenTrace? trace = null)
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

            if (TryRunPushCollisionHooks(actor, destination, direction, trace))
            {
                return true;
            }

            if (Grid.IsBlocked(destination) || Grid.IsOccupied(destination))
            {
                return moved;
            }
        }

        return moved;
    }

    public EffectCommandResult ResolveEffectCommand(EffectCommand command, OmenTrace? trace = null)
    {
        switch (command)
        {
            case DamageActorCommand damage:
                DamageResolution damageResult = ResolveDamageActor(damage.ActorId, damage.Amount, trace);
                return EffectCommandResult.Damage(
                    damageResult.ChangedWorld,
                    damageResult.AppliedAmount,
                    damageResult.WasPrevented);

            case PushActorCommand push:
                return new EffectCommandResult(TryPushActor(push.ActorId, push.Direction, push.Distance, trace));

            case SetTileStateCommand setTile:
                return new EffectCommandResult(TrySetTileState(setTile.Position, setTile.State));

            case ModifyActorCounterCommand counter:
                int actorCounterBefore = GetActorCounter(counter.ActorId, counter.CounterId);
                int actorCounter = AddActorCounter(counter.ActorId, counter.CounterId, counter.Amount);
                return new EffectCommandResult(actorCounter != actorCounterBefore, actorCounter);

            case ModifyTileCounterCommand counter:
                int tileCounterBefore = GetTileCounter(counter.Position, counter.CounterId);
                int tileCounter = AddTileCounter(counter.Position, counter.CounterId, counter.Amount);
                return new EffectCommandResult(tileCounter != tileCounterBefore, tileCounter);

            case ModifyEffectCounterCommand counter:
                int effectCounterBefore = counter.Effect.Counters.Get(counter.CounterId);
                int effectCounter;

                if (counter.CounterId == "counter.stack"
                    && counter.Amount > 0
                    && EffectDefinitionCatalog.TryGet(
                        counter.Effect.EffectId,
                        out EffectDefinition? effectDefinition)
                    && effectDefinition.MaxStacks.HasValue)
                {
                    effectCounter = counter.Effect.Counters.Set(
                        counter.CounterId,
                        (int)Math.Min(
                            effectDefinition.MaxStacks.Value,
                            (long)effectCounterBefore + counter.Amount));
                }
                else
                {
                    effectCounter = counter.Effect.Counters.Add(counter.CounterId, counter.Amount);
                }

                return new EffectCommandResult(effectCounter != effectCounterBefore, effectCounter);

            case AttachActorEffectCommand attach:
                EffectAttachmentResult actorAttachment = AttachEffectToActorDetailed(
                    attach.TargetActorId,
                    attach.EffectId,
                    attach.OwnerActorId,
                    attach.Stacks,
                    trace);
                return !actorAttachment.ChangedWorld
                    ? EffectCommandResult.NoChange
                    : EffectCommandResult.Changed(effect: actorAttachment.Effect);

            case AttachTileEffectCommand attach:
                EffectAttachmentResult tileAttachment = AttachEffectToTileDetailed(
                    attach.Position,
                    attach.EffectId,
                    attach.OwnerActorId,
                    attach.Stacks,
                    trace);
                return !tileAttachment.ChangedWorld
                    ? EffectCommandResult.NoChange
                    : EffectCommandResult.Changed(effect: tileAttachment.Effect);

            case DetachActorEffectCommand detach:
                return new EffectCommandResult(DetachEffectFromActor(detach.TargetActorId, detach.InstanceId));

            case RemoveTileEffectCommand detach:
                return new EffectCommandResult(DetachEffectFromTile(detach.Position, detach.InstanceId));

            default:
                return EffectCommandResult.NoChange;
        }
    }

    public TacticalEncounter Clone()
    {
        var actorClones = _actorsById.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone());
        var tileCounterClones = _tileCounters.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone());
        var tileEffectClones = _tileEffects.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(effect => effect.Clone()).ToList());
        var temporaryRaisedStoneClones = new Dictionary<GridPos, int>(_temporaryRaisedStoneRounds);

        return new TacticalEncounter(
            Grid.Clone(),
            Turns.Clone(),
            FloorRules,
            actorClones,
            tileCounterClones,
            tileEffectClones,
            temporaryRaisedStoneClones,
            _relicIds.ToList(),
            _nextActorId,
            _nextTileEffectInstanceId,
            Player.Id,
            VictoryTargetEnemyId);
    }

    private void TickTemporaryRaisedStone()
    {
        foreach ((GridPos position, int rounds) in _temporaryRaisedStoneRounds.ToArray())
        {
            if (Grid.GetTile(position) != TileState.RaisedStone)
            {
                _temporaryRaisedStoneRounds.Remove(position);
                continue;
            }

            if (rounds > 1)
            {
                _temporaryRaisedStoneRounds[position] = rounds - 1;
                continue;
            }

            TrySetTileState(position, TileState.Floor);
        }
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

    public void AddRelic(string relicId)
    {
        if (!_relicIds.Contains(relicId, StringComparer.Ordinal))
        {
            _relicIds.Add(relicId);
        }
    }

    public bool RemoveRelic(string relicId)
    {
        return _relicIds.Remove(relicId);
    }

    public bool IsEnemyEngaged(EncounterActor enemy)
    {
        return enemy.IsAlerted
            && enemy.IsAlive
            && enemy.Faction == Faction.Enemy
            && IsWithinAwarenessRadius(enemy.Position, Player.Position);
    }

    private void UpdateEnemyAwareness()
    {
        EncounterActor[] enemies = Enemies.OrderBy(enemy => enemy.Id).ToArray();

        foreach (EncounterActor enemy in enemies)
        {
            if (IsWithinAwarenessRadius(enemy.Position, Player.Position)
                && Grid.HasLineOfSight(enemy.Position, Player.Position))
            {
                enemy.IsAlerted = true;
            }
        }

        bool changed;

        do
        {
            changed = false;

            foreach (EncounterActor enemy in enemies.Where(enemy => !enemy.IsAlerted))
            {
                if (!IsWithinAwarenessRadius(enemy.Position, Player.Position))
                {
                    continue;
                }

                bool alertedByAlly = enemies.Any(ally =>
                    ally.IsAlerted
                    && IsAlly(enemy, ally)
                    && IsWithinAwarenessRadius(enemy.Position, ally.Position)
                    && Grid.HasLineOfSight(enemy.Position, ally.Position));

                if (alertedByAlly)
                {
                    enemy.IsAlerted = true;
                    changed = true;
                }
            }
        }
        while (changed);
    }

    private static bool IsWithinAwarenessRadius(GridPos left, GridPos right)
    {
        return Math.Max(Math.Abs(left.X - right.X), Math.Abs(left.Y - right.Y)) <= EnemyAwarenessRadius;
    }

    private bool TryRunPushCollisionHooks(EncounterActor actor, GridPos destination, Direction direction, OmenTrace? trace)
    {
        OmenTrace collisionTrace = trace ?? new OmenTrace();
        bool changedWorld = RunRuleHooks(
            RuleTriggerIds.PushCollision,
            collisionTrace,
            eventActor: actor,
            eventTile: destination,
            eventDirection: direction).ChangedWorld;
        changedWorld |= RunRelicHooks(
            RuleTriggerIds.PushCollision,
            collisionTrace,
            eventActor: actor,
            eventTile: destination,
            eventDirection: direction).ChangedWorld;
        return changedWorld;
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
        RunEffectTrigger(actor, EffectTriggerIds.Move);

        if (!actor.IsAlive)
        {
            return true;
        }

        RunRelicHooks(
            EffectTriggerIds.Move,
            new OmenTrace(),
            eventActor: actor,
            eventTile: destination,
            eventDirection: direction);

        if (!actor.IsAlive)
        {
            return true;
        }

        RunBecameAdjacentHooks(actor, origin);

        if (!actor.IsAlive)
        {
            return true;
        }

        RunRuleHooks(
            RuleTriggerIds.AfterMove,
            new OmenTrace(),
            eventActor: actor,
            eventTile: destination,
            eventDirection: direction);

        if (!actor.IsAlive)
        {
            return true;
        }

        RunRelicHooks(
            RuleTriggerIds.AfterMove,
            new OmenTrace(),
            eventActor: actor,
            eventTile: destination,
            eventDirection: direction);
        return true;
    }

    private void RemoveTileEffectsOwnedBy(int ownerActorId)
    {
        foreach ((GridPos position, List<EffectInstance> effects) in _tileEffects.ToArray())
        {
            effects.RemoveAll(effect => effect.OwnerActorId == ownerActorId);

            if (effects.Count == 0)
            {
                _tileEffects.Remove(position);
            }
        }
    }

    private void TakeDummyEnemyTurn(EncounterActor enemy)
    {
        if (enemy.EnemyId == null || !EnemyConfigCatalog.TryGet(enemy.EnemyId, out EnemyConfig? config))
        {
            return;
        }

        var working = new WorkingContext
        {
            CasterActorId = enemy.Id,
            SelectedTarget = enemy.Position,
            StepLimit = 48
        };

        new BehaviorMachine().Execute(
            config.BehaviorId,
            new BehaviorExecutionContext
            {
                SpellWorld = new EncounterSpellWorld(this),
                Encounter = this,
                Working = working,
                Caster = enemy,
                Enemy = enemy,
                Trace = new OmenTrace()
            });
    }

    private BeforeDamageResult RunBeforeDamageHooks(EncounterActor target, int amount, OmenTrace? trace)
    {
        int nextAmount = amount;
        bool changedWorld = false;

        foreach (EffectInstance instance in target.Effects.ToArray())
        {
            HookExecutionResult effectResult = RunEffectTrigger(
                target,
                instance,
                EffectTriggerIds.BeforeDamage,
                eventActor: target,
                eventDamage: nextAmount,
                trace: trace);
            nextAmount = effectResult.Context.EventDamage;
            changedWorld |= effectResult.ChangedWorld;

            if (nextAmount <= 0)
            {
                return new BeforeDamageResult(0, changedWorld);
            }
        }

        HookExecutionResult ruleResult = RunRuleHooks(
            RuleTriggerIds.BeforeDamage,
            trace ?? new OmenTrace(),
            eventActor: target,
            eventDamage: nextAmount);
        nextAmount = ruleResult.Context.EventDamage;
        changedWorld |= ruleResult.ChangedWorld;

        if (nextAmount <= 0)
        {
            return new BeforeDamageResult(0, changedWorld);
        }

        HookExecutionResult relicResult = RunRelicHooks(
            EffectTriggerIds.BeforeDamage,
            trace ?? new OmenTrace(),
            eventActor: target,
            eventDamage: nextAmount);
        changedWorld |= relicResult.ChangedWorld;
        return new BeforeDamageResult(relicResult.Context.EventDamage, changedWorld);
    }

    private void RunBecameAdjacentHooks(EncounterActor movedActor, GridPos origin)
    {
        foreach (EncounterActor target in Actors.Where(actor => actor.IsAlive && actor.Id != movedActor.Id).ToArray())
        {
            if (!movedActor.IsAlive)
            {
                return;
            }

            bool wasAdjacent = origin.ManhattanDistanceTo(target.Position) == 1;
            bool isAdjacent = movedActor.Position.ManhattanDistanceTo(target.Position) == 1;

            if (wasAdjacent || !isAdjacent)
            {
                continue;
            }

            foreach (EffectInstance instance in target.Effects.ToArray())
            {
                RunEffectTrigger(target, instance, EffectTriggerIds.ActorBecameAdjacent, movedActor);

                if (!movedActor.IsAlive || !target.IsAlive)
                {
                    break;
                }
            }

            if (!movedActor.IsAlive)
            {
                return;
            }
        }
    }

    private bool RunAfterSpellResolvedHooks(OmenTrace trace)
    {
        bool changedWorld = false;

        foreach (EncounterActor actor in Actors.Where(actor => actor.IsAlive).ToArray())
        {
            changedWorld |= RunEffectTrigger(actor, EffectTriggerIds.AfterSpellResolved, trace).ChangedWorld;
        }

        changedWorld |= RunRuleHooks(
            RuleTriggerIds.AfterSpellResolved,
            trace,
            eventActor: Player).ChangedWorld;
        changedWorld |= RunRelicHooks(
            EffectTriggerIds.AfterSpellResolved,
            trace,
            eventActor: Player).ChangedWorld;

        return changedWorld;
    }

    private HookExecutionResult RunRuleHooks(
        string trigger,
        OmenTrace trace,
        EncounterActor? eventActor = null,
        GridPos? eventTile = null,
        Direction? eventDirection = null,
        int eventDamage = 0)
    {
        var context = new BehaviorExecutionContext
        {
            Encounter = this,
            EventActor = eventActor,
            EventTile = eventTile,
            EventDirection = eventDirection,
            EventDamage = eventDamage,
            Trace = trace
        };

        return RunHookBehaviors(FloorRules.GetBehaviorIds(trigger), context);
    }

    private HookExecutionResult RunRelicHooks(
        string trigger,
        OmenTrace trace,
        EncounterActor? eventActor = null,
        GridPos? eventTile = null,
        Direction? eventDirection = null,
        int eventDamage = 0)
    {
        var context = new BehaviorExecutionContext
        {
            Encounter = this,
            EventActor = eventActor,
            EventTile = eventTile,
            EventDirection = eventDirection,
            EventDamage = eventDamage,
            Trace = trace
        };

        IEnumerable<string> behaviorIds = _relicIds
            .Select(id => RelicDefinitionCatalog.TryGet(id, out RelicDefinition? relic) ? relic : null)
            .OfType<RelicDefinition>()
            .SelectMany(relic => relic.GetBehaviorIds(trigger));

        return RunHookBehaviors(behaviorIds, context);
    }

    private HookExecutionResult RunHookBehaviors(
        IEnumerable<string> behaviorIds,
        BehaviorExecutionContext context)
    {
        if (!_hookRecursionGuard.TryEnter(context.Trace, out IDisposable? recursionScope))
        {
            return new HookExecutionResult(context, false);
        }

        using (recursionScope)
        {
            bool changedWorld = false;
            var machine = new BehaviorMachine();

            foreach (string behaviorId in behaviorIds)
            {
                BehaviorExecutionResult result = machine.Execute(behaviorId, context);
                changedWorld |= result.ChangedWorld;
            }

            return new HookExecutionResult(context, changedWorld);
        }
    }

    private HookExecutionResult RunEffectBehavior(
        EncounterActor target,
        EffectInstance instance,
        string behaviorId,
        EncounterActor? eventActor = null,
        int eventDamage = 0,
        OmenTrace? trace = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return new HookExecutionResult(new BehaviorExecutionContext
            {
                Encounter = this,
                EventActor = eventActor ?? target,
                EventDamage = eventDamage,
                EffectTarget = target,
                Effect = instance,
                Trace = trace ?? new OmenTrace()
            }, false);
        }

        var context = new BehaviorExecutionContext
        {
            Encounter = this,
            EventActor = eventActor ?? target,
            EventDamage = eventDamage,
            EffectTarget = target,
            Effect = instance,
            Trace = trace ?? new OmenTrace()
        };

        if (!_hookRecursionGuard.TryEnter(context.Trace, out IDisposable? recursionScope))
        {
            return new HookExecutionResult(context, false);
        }

        using (recursionScope)
        {
            BehaviorExecutionResult result = new BehaviorMachine().Execute(
                behaviorId,
                context);

            return new HookExecutionResult(context, result.ChangedWorld);
        }
    }

    private HookExecutionResult RunEffectTrigger(
        EncounterActor target,
        string triggerId,
        OmenTrace? trace = null)
    {
        HookExecutionResult result = RunEffectBehavior(
            target,
            new EffectInstance(0, "", target.Id),
            "",
            trace: trace);
        bool changedWorld = false;

        foreach (EffectInstance instance in target.Effects.ToArray())
        {
            result = RunEffectTrigger(target, instance, triggerId, trace: trace);
            changedWorld |= result.ChangedWorld;

            if (!target.IsAlive)
            {
                break;
            }
        }

        return new HookExecutionResult(result.Context, changedWorld);
    }

    private HookExecutionResult RunEffectTrigger(
        EncounterActor target,
        EffectInstance instance,
        string triggerId,
        EncounterActor? eventActor = null,
        int eventDamage = 0,
        OmenTrace? trace = null)
    {
        if (!EffectDefinitionCatalog.TryGet(instance.EffectId, out EffectDefinition? definition))
        {
            return RunEffectBehavior(target, instance, "", eventActor, eventDamage, trace);
        }

        HookExecutionResult result = RunEffectBehavior(target, instance, "", eventActor, eventDamage, trace);
        bool changedWorld = false;

        foreach (string behaviorId in definition.GetBehaviorIds(triggerId))
        {
            result = RunEffectBehavior(target, instance, behaviorId, eventActor, result.Context.EventDamage, trace);
            changedWorld |= result.ChangedWorld;

            if (!target.IsAlive || eventActor?.IsAlive == false)
            {
                break;
            }
        }

        return new HookExecutionResult(result.Context, changedWorld);
    }

    private EffectInstance GetOrAddTileEffect(GridPos position, string effectId, int ownerActorId)
    {
        if (!_tileEffects.TryGetValue(position, out List<EffectInstance>? effects))
        {
            effects = new List<EffectInstance>();
            _tileEffects[position] = effects;
        }

        EffectInstance? existing = effects.FirstOrDefault(effect =>
            effect.EffectId == effectId && effect.OwnerActorId == ownerActorId);

        if (existing != null)
        {
            return existing;
        }

        var instance = new EffectInstance(_nextTileEffectInstanceId++, effectId, ownerActorId);
        effects.Add(instance);
        return instance;
    }

    private static int AddStartingStacks(EffectInstance instance, EffectDefinition definition, int stacks)
    {
        if (stacks <= 0 || !definition.AllowsCounter("counter.stack"))
        {
            return 0;
        }

        int before = instance.Counters.Get("counter.stack");
        long uncapped = (long)before + stacks;
        int next = definition.MaxStacks.HasValue
            ? (int)Math.Min(definition.MaxStacks.Value, uncapped)
            : (int)Math.Min(int.MaxValue, uncapped);
        instance.Counters.Set("counter.stack", next);
        return next - before;
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

    private static bool IsCondition(string effectId)
    {
        return effectId.StartsWith("condition.", StringComparison.Ordinal);
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

    private readonly record struct HookExecutionResult(BehaviorExecutionContext Context, bool ChangedWorld);

    private readonly record struct BeforeDamageResult(int Amount, bool ChangedWorld);

    private readonly record struct DamageResolution(bool ChangedWorld, int AppliedAmount, bool WasPrevented)
    {
        public static DamageResolution NoChange { get; } = new(false, 0, false);
    }

    private readonly record struct EffectAttachmentResult(
        EffectInstance? Effect,
        bool ChangedWorld,
        int StacksAdded)
    {
        public static EffectAttachmentResult NoChange { get; } = new(null, false, 0);
    }
}

public sealed class EffectHookRecursionGuard
{
    public const int MaximumDepth = 16;
    public const int MaximumInvocationsPerChain = 64;

    private int _depth;
    private int _invocationsInChain;

    public bool TryEnter(OmenTrace trace, out IDisposable? scope)
    {
        if (_depth == 0)
        {
            _invocationsInChain = 0;
        }

        if (_depth >= MaximumDepth || _invocationsInChain >= MaximumInvocationsPerChain)
        {
            trace.Add("Effect hook recursion limit reached; the nested hook was ignored.");
            scope = null;
            return false;
        }

        _depth++;
        _invocationsInChain++;
        scope = new RecursionScope(this);
        return true;
    }

    private void Exit()
    {
        _depth = Math.Max(0, _depth - 1);

        if (_depth == 0)
        {
            _invocationsInChain = 0;
        }
    }

    private sealed class RecursionScope : IDisposable
    {
        private EffectHookRecursionGuard? _owner;

        public RecursionScope(EffectHookRecursionGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner?.Exit();
            _owner = null;
        }
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
    public IReadOnlyList<EffectInstance> Effects => _effects;
    public bool IsAlive => Health > 0;
    public bool IsAlerted { get; internal set; }

    private readonly List<EffectInstance> _effects = new();
    private readonly Dictionary<string, WorkingReference> _workingReferences = new(StringComparer.Ordinal);
    private int _nextEffectInstanceId = 1;

    public bool StoreWorkingReference(string refId, WorkingReference reference)
    {
        if (string.IsNullOrWhiteSpace(refId)
            || (_workingReferences.TryGetValue(refId, out WorkingReference existing)
                && existing == reference))
        {
            return false;
        }

        _workingReferences[refId] = reference;
        return true;
    }

    public bool TryGetWorkingReference(string refId, out WorkingReference reference)
    {
        return _workingReferences.TryGetValue(refId, out reference);
    }

    public void ApplyDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        Health = (int)Math.Min(MaxHealth, (long)Health + Math.Max(0L, amount));
    }

    public EncounterActor Clone()
    {
        var clone = new EncounterActor(Id, Faction, Position, MaxHealth, EnemyId)
        {
            Health = Health,
            IsAlerted = IsAlerted
        };

        foreach ((string counterId, int amount) in Counters.All)
        {
            clone.Counters.Add(counterId, amount);
        }

        foreach ((string refId, WorkingReference reference) in _workingReferences)
        {
            clone._workingReferences.Add(refId, reference);
        }

        clone._nextEffectInstanceId = _nextEffectInstanceId;

        foreach (EffectInstance effect in _effects)
        {
            clone._effects.Add(effect.Clone());
        }

        return clone;
    }

    public EffectInstance GetOrAddEffect(string effectId, int ownerActorId)
    {
        EffectInstance? existing = _effects.FirstOrDefault(effect =>
            effect.EffectId == effectId && effect.OwnerActorId == ownerActorId);

        if (existing != null)
        {
            return existing;
        }

        var instance = new EffectInstance(_nextEffectInstanceId++, effectId, ownerActorId);
        _effects.Add(instance);
        return instance;
    }

    public EffectInstance? FindEffect(string effectId)
    {
        return _effects.FirstOrDefault(effect => effect.EffectId == effectId);
    }

    public EffectInstance? FindEffect(string effectId, int ownerActorId)
    {
        return _effects.FirstOrDefault(effect => effect.EffectId == effectId && effect.OwnerActorId == ownerActorId);
    }

    public bool RemoveEffect(int instanceId)
    {
        return _effects.RemoveAll(effect => effect.InstanceId == instanceId) > 0;
    }
}

public readonly record struct TileCondition(GridPos Position, string ConditionId, int OwnerActorId);

public sealed class EffectInstance
{
    public EffectInstance(int instanceId, string effectId, int ownerActorId)
    {
        InstanceId = instanceId;
        EffectId = effectId;
        OwnerActorId = ownerActorId;
    }

    public int InstanceId { get; }
    public string EffectId { get; }
    public int OwnerActorId { get; }
    public CounterSet Counters { get; } = new();

    public EffectInstance Clone()
    {
        var clone = new EffectInstance(InstanceId, EffectId, OwnerActorId);

        foreach ((string counterId, int amount) in Counters.All)
        {
            clone.Counters.Add(counterId, amount);
        }

        return clone;
    }
}
