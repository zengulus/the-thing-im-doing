using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public sealed class WorkingPreview
{
    public WorkingPreview(WorkingResult result, TacticalEncounter encounter)
    {
        Result = result;
        Encounter = encounter;
    }

    public WorkingResult Result { get; }
    public TacticalEncounter Encounter { get; }
}
