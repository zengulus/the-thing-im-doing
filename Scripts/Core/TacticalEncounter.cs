using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.World;

namespace TheThingImDoing.Core;

public sealed class TacticalEncounter
{
    private readonly Dictionary<int, EncounterActor> _actorsById = new();
    private int _nextActorId = 1;

    public TacticalEncounter(int width, int height, GridPos playerStart)
    {
        Grid = new TacticalGrid(width, height);
        Turns = new TurnSystem();
        Player = AddActor(Faction.Player, playerStart, health: 5);
    }

    public TacticalGrid Grid { get; }
    public TurnSystem Turns { get; }
    public EncounterActor Player { get; }

    public IReadOnlyCollection<EncounterActor> Actors => _actorsById.Values;

    public IEnumerable<EncounterActor> Enemies
    {
        get
        {
            return _actorsById.Values.Where(actor => actor.Faction == Faction.Enemy && actor.IsAlive);
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
        return AddActor(Faction.Enemy, position, health);
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

    private EncounterActor AddActor(Faction faction, GridPos position, int health)
    {
        int actorId = _nextActorId++;
        var actor = new EncounterActor(actorId, faction, position, health);

        if (!Grid.TryAddActor(actorId, position))
        {
            throw new InvalidOperationException($"Could not place actor {actorId} at {position}.");
        }

        _actorsById.Add(actorId, actor);
        return actor;
    }

    private bool TryMoveActor(EncounterActor actor, Direction direction)
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
        if (enemy.Position.ManhattanDistanceTo(Player.Position) == 1)
        {
            Player.ApplyDamage(1);
            return;
        }

        foreach (Direction direction in GetDirectionsToward(enemy.Position, Player.Position))
        {
            if (TryMoveActor(enemy, direction))
            {
                return;
            }
        }
    }

    private static IEnumerable<Direction> GetDirectionsToward(GridPos from, GridPos to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx > 0)
            {
                yield return Direction.East;
            }
            else if (dx < 0)
            {
                yield return Direction.West;
            }

            if (dy > 0)
            {
                yield return Direction.South;
            }
            else if (dy < 0)
            {
                yield return Direction.North;
            }
        }
        else
        {
            if (dy > 0)
            {
                yield return Direction.South;
            }
            else if (dy < 0)
            {
                yield return Direction.North;
            }

            if (dx > 0)
            {
                yield return Direction.East;
            }
            else if (dx < 0)
            {
                yield return Direction.West;
            }
        }
    }
}

public sealed class EncounterActor
{
    public EncounterActor(int id, Faction faction, GridPos position, int health)
    {
        Id = id;
        Faction = faction;
        Position = position;
        Health = health;
    }

    public int Id { get; }
    public Faction Faction { get; }
    public GridPos Position { get; internal set; }
    public int Health { get; private set; }
    public bool IsAlive => Health > 0;

    public void ApplyDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
    }
}

