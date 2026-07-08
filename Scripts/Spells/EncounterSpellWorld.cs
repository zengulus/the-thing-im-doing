using System.Collections.Generic;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;
using TheThingImDoing.World;

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

    public EncounterActor? GetNearestActor(EncounterActor source, string relation)
    {
        return _encounter.GetNearestActor(source, relation);
    }

    public IEnumerable<EncounterActor> GetActorsByRelation(EncounterActor source, string relation)
    {
        return _encounter.GetActorsByRelation(source, relation);
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

    public LingeringEffectInstance? AttachLingeringEffect(EncounterActor target, string effectId, EncounterActor owner, int stacks)
    {
        return _encounter.AttachLingeringEffect(target.Id, effectId, owner.Id, stacks);
    }

    public void ApplyDamage(EncounterActor target, int amount, EncounterActor? source)
    {
        _encounter.TryDamageActor(target.Id, amount);
    }

    public bool TryPushActor(EncounterActor target, Direction direction, int distance, EncounterActor? source)
    {
        return _encounter.TryPushActor(target.Id, direction, distance);
    }

    public bool CanSetTileState(GridPos pos, TileState state)
    {
        return _encounter.CanSetTileState(pos, state);
    }

    public void SetTileState(GridPos pos, TileState state)
    {
        _encounter.TrySetTileState(pos, state);
    }
}
