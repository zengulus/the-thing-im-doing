using System.Collections.Generic;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public interface ISpellWorld
{
    bool IsInside(GridPos pos);
    bool IsOccupied(GridPos pos);
    bool IsClear(GridPos pos);

    EncounterActor? GetActor(int actorId);
    EncounterActor? GetActorAt(GridPos pos);
    EncounterActor? GetNearestEnemy(EncounterActor caster);
    IEnumerable<EncounterActor> GetEnemiesOf(EncounterActor caster);

    bool HasMark(GridPos pos, int ownerActorId);
    bool HasMark(EncounterActor target, int ownerActorId);
    void AddMark(GridPos pos, int ownerActorId, int durationTurns = 2);
    void AddMark(EncounterActor target, int ownerActorId, int durationTurns = 2);

    int GetCounter(EncounterActor target, string counterId);
    int AddCounter(EncounterActor target, string counterId, int amount);
    int GetCounter(GridPos pos, string counterId);
    int AddCounter(GridPos pos, string counterId, int amount);

    void ApplyDamage(EncounterActor target, int amount, EncounterActor? source);
    bool TryPushActor(EncounterActor target, Direction direction, int distance, EncounterActor? source);

    bool CanRaiseStone(GridPos pos);
    void RaiseStone(GridPos pos, int durationTurns = 2);
}
