using System.Collections.Generic;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;
using TheThingImDoing.World;

namespace TheThingImDoing.Spells;

public interface ISpellWorld
{
    bool IsInside(GridPos pos);
    bool IsOccupied(GridPos pos);
    bool IsClear(GridPos pos);
    bool IsWithinPerceptionRange(GridPos from, GridPos to, int radius);
    bool HasLineOfSight(GridPos from, GridPos to);

    EncounterActor? GetActor(int actorId);
    EncounterActor? GetActorAt(GridPos pos);
    EncounterActor? GetNearestActor(EncounterActor source, string relation);
    IEnumerable<EncounterActor> GetActorsByRelation(EncounterActor source, string relation);

    int GetCounter(EncounterActor target, string counterId);
    int AddCounter(EncounterActor target, string counterId, int amount);
    int GetCounter(GridPos pos, string counterId);
    int AddCounter(GridPos pos, string counterId, int amount);
    EffectInstance? AttachEffect(EncounterActor target, string effectId, EncounterActor owner, int stacks);
    EffectInstance? AttachEffect(GridPos pos, string effectId, EncounterActor owner, int stacks);
    bool HasEffect(EncounterActor target, string effectId, EncounterActor owner);
    bool HasEffect(GridPos pos, string effectId, EncounterActor owner);
    EffectCommandResult Resolve(EffectCommand command, OmenTrace? trace = null);

    void ApplyDamage(EncounterActor target, int amount, EncounterActor? source);
    bool TryPushActor(EncounterActor target, Direction direction, int distance, EncounterActor? source);

    bool CanSetTileState(GridPos pos, TileState state);
    void SetTileState(GridPos pos, TileState state);
}
