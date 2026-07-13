using TheThingImDoing.World;

namespace TheThingImDoing.Core;

public static class TacticalEncounterFactory
{
    public static TacticalEncounter Create(
        string encounterId,
        int playerHealth = 5,
        int playerMaxHealth = 5,
        int? proceduralSeed = null)
    {
        return Create(
            EncounterDefinitionCatalog.Get(encounterId),
            playerHealth,
            playerMaxHealth,
            proceduralSeed);
    }

    public static TacticalEncounter Create(
        EncounterDefinition definition,
        int playerHealth = 5,
        int playerMaxHealth = 5,
        int? proceduralSeed = null)
    {
        EnvironmentDefinition environment = EnvironmentDefinitionCatalog.Get(definition.EnvironmentId);
        EncounterLayout layout = ResolveLayout(definition, proceduralSeed);
        var encounter = new TacticalEncounter(
            layout.Width,
            layout.Height,
            layout.PlayerStart,
            environment.FloorRuleId,
            playerHealth,
            playerMaxHealth);

        foreach (EncounterTilePlacement tile in layout.Tiles)
        {
            encounter.Grid.SetTile(tile.Position, tile.State);
        }

        foreach (EncounterEnemyPlacement enemy in layout.Enemies)
        {
            encounter.AddEnemy(enemy.EnemyId, enemy.Position);
        }

        return encounter;
    }

    public static EncounterLayout ResolveLayout(
        EncounterDefinition definition,
        int? proceduralSeed = null)
    {
        return definition.Generation == null
            ? EncounterLayout.FromAuthored(definition)
            : ProceduralEncounterGenerator.Generate(definition, proceduralSeed);
    }
}
