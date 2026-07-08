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
    public EncounterActor? EffectTarget { get; init; }
    public EffectInstance? Effect { get; init; }
    public required OmenTrace Trace { get; init; }

    public EffectCommandResult Resolve(EffectCommand command)
    {
        if (command is PreventEventDamageCommand)
        {
            if (EventDamage <= 0)
            {
                return EffectCommandResult.NoChange;
            }

            EventDamage = 0;
            return EffectCommandResult.Changed();
        }

        if (SpellWorld != null)
        {
            return SpellWorld.Resolve(command);
        }

        return Encounter?.ResolveEffectCommand(command) ?? EffectCommandResult.NoChange;
    }
}
