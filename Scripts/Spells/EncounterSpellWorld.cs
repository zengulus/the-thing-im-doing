using System.Collections.Generic;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public sealed class EncounterSpellWorld : ISpellWorld
{
    private readonly TacticalEncounter _encounter;

    public EncounterSpellWorld(TacticalEncounter encounter)
    {
        _encounter = encounter;
    }

    public bool IsInside(GridPos pos)
    {
        return _encounter.Grid.IsInside(pos);
    }

    public bool IsOccupied(GridPos pos)
    {
        return _encounter.Grid.IsOccupied(pos);
    }

    public bool IsClear(GridPos pos)
    {
        return _encounter.Grid.IsEmpty(pos);
    }

    public EncounterActor? GetActor(int actorId)
    {
        return _encounter.GetActor(actorId);
    }

    public EncounterActor? GetActorAt(GridPos pos)
    {
        return _encounter.GetActorAt(pos);
    }

    public EncounterActor? GetNearestEnemy(EncounterActor caster)
    {
        return _encounter.GetNearestEnemy(caster);
    }

    public IEnumerable<EncounterActor> GetEnemiesOf(EncounterActor caster)
    {
        return _encounter.Enemies;
    }

    public bool HasMark(GridPos pos, int ownerActorId)
    {
        return _encounter.HasTileMark(pos, ownerActorId);
    }

    public bool HasMark(EncounterActor target, int ownerActorId)
    {
        return _encounter.HasActorMark(target.Id, ownerActorId);
    }

    public void AddMark(GridPos pos, int ownerActorId, int durationTurns = 2)
    {
        _encounter.AddTileMark(pos, ownerActorId);
    }

    public void AddMark(EncounterActor target, int ownerActorId, int durationTurns = 2)
    {
        _encounter.AddActorMark(target.Id, ownerActorId);
    }

    public int GetCounter(EncounterActor target, string counterId)
    {
        return _encounter.GetActorCounter(target.Id, counterId);
    }

    public int AddCounter(EncounterActor target, string counterId, int amount)
    {
        return _encounter.AddActorCounter(target.Id, counterId, amount);
    }

    public int GetCounter(GridPos pos, string counterId)
    {
        return _encounter.GetTileCounter(pos, counterId);
    }

    public int AddCounter(GridPos pos, string counterId, int amount)
    {
        return _encounter.AddTileCounter(pos, counterId, amount);
    }

    public void ApplyDamage(EncounterActor target, int amount, EncounterActor? source)
    {
        _encounter.TryDamageActor(target.Id, amount);
    }

    public bool TryPushActor(EncounterActor target, Direction direction, int distance, EncounterActor? source)
    {
        return _encounter.TryPushActor(target.Id, direction, distance);
    }

    public bool CanRaiseStone(GridPos pos)
    {
        return _encounter.CanRaiseStone(pos);
    }

    public void RaiseStone(GridPos pos, int durationTurns = 2)
    {
        _encounter.TryRaiseStone(pos);
    }
}
