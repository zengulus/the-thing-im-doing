using System.Linq;
using TheThingImDoing.Relics;
using TheThingImDoing.Spells;

namespace TheThingImDoing.Progression;

public static class SandboxStartingState
{
    public const string RunId = "run.archive_sandbox";
    public const int MaxHealth = 8;

    public static RunPlayerState Create()
    {
        return new RunPlayerState(
            MaxHealth,
            ClauseDefinitionCatalog.All.Select(definition => definition.Id),
            RelicDefinitionCatalog.All.Select(definition => definition.Id));
    }
}
