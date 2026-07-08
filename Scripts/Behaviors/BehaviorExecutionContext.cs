using TheThingImDoing.Actors;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;

namespace TheThingImDoing.Behaviors;

public sealed class BehaviorExecutionContext
{
    public ISpellWorld? SpellWorld { get; init; }
    public TacticalEncounter? Encounter { get; init; }
    public WorkingContext? Working { get; init; }
    public EncounterActor? Caster { get; init; }
    public EncounterActor? Enemy { get; init; }
    public EncounterActor? EventActor { get; init; }
    public GridPos? EventTile { get; init; }
    public Direction? EventDirection { get; init; }
    public int EventDamage { get; set; }
    public EncounterActor? LingeringTarget { get; init; }
    public LingeringEffectInstance? LingeringEffect { get; init; }
    public required OmenTrace Trace { get; init; }
}
