using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.Content;
using TheThingImDoing.World;

namespace TheThingImDoing.Core;

public sealed record EncounterDefinition(
    string Id,
    string DisplayNameKey,
    string EnvironmentId,
    int GridWidth,
    int GridHeight,
    GridPos PlayerStart,
    IReadOnlyList<EncounterEnemyPlacement> Enemies,
    IReadOnlyList<EncounterTilePlacement> Tiles,
    bool IsFinal,
    int RewardAmount,
    IReadOnlyList<string> Tags,
    EncounterGenerationDefinition? Generation = null,
    string? VictoryTargetEnemyId = null)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
}

public sealed record EncounterEnemyPlacement(string EnemyId, GridPos Position);

public sealed record EncounterTilePlacement(GridPos Position, TileState State);

public sealed record EncounterGenerationDefinition(
    int Seed,
    int Width,
    int Height,
    int RoomCount,
    int EnemyCount,
    int MinRoomWidth,
    int MinRoomHeight,
    int MaxRoomWidth,
    int MaxRoomHeight,
    int CorridorWidth,
    int ExtraConnectionPercent,
    int TerrainVariationPercent,
    IReadOnlyList<TileState> TerrainStates);

public static class EncounterDefinitionCatalog
{
    private const int MaximumGridSize = 64;
    private static readonly Lazy<ContentRegistryResult<EncounterDefinition>> Registry = new(LoadRegistry);

    public static IEnumerable<EncounterDefinition> All => Registry.Value.Definitions.Values;

    public static EncounterDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out EncounterDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<EncounterDefinition> LoadRegistry()
    {
        ContentRegistryResult<EncounterDefinition> result = ContentRegistry.Build(
            "encounter",
            ContentJsonLoader.LoadItemsWithSources<EncounterContentFile, EncounterContentDefinition>(
                "encounters.json",
                file => file.Encounters),
            Resolve);

        return new ContentRegistryResult<EncounterDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<EncounterDefinition> Resolve(EncounterContentDefinition content)
    {
        var issues = new List<string>();
        var enemies = new List<EncounterEnemyPlacement>();
        var tiles = new List<EncounterTilePlacement>();
        string environmentId = (content.EnvironmentId ?? "").Trim();
        string? victoryTargetEnemyId = string.IsNullOrWhiteSpace(content.VictoryTargetEnemyId)
            ? null
            : content.VictoryTargetEnemyId.Trim();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        ValidateEnvironmentReference(environmentId, issues);

        if (content.GridWidth <= 0 || content.GridWidth > MaximumGridSize)
        {
            issues.Add($"gridWidth must be between 1 and {MaximumGridSize}.");
        }

        if (content.GridHeight <= 0 || content.GridHeight > MaximumGridSize)
        {
            issues.Add($"gridHeight must be between 1 and {MaximumGridSize}.");
        }

        GridPositionContentDefinition? playerStartContent = content.PlayerStart;

        if (playerStartContent == null)
        {
            issues.Add("playerStart is required.");
        }

        var playerStart = new GridPos(playerStartContent?.X ?? -1, playerStartContent?.Y ?? -1);

        if (!IsInside(playerStart, content.GridWidth, content.GridHeight))
        {
            issues.Add($"playerStart {playerStart} is outside the grid.");
        }

        if (content.RewardAmount < 0)
        {
            issues.Add("rewardAmount cannot be negative.");
        }

        IReadOnlyList<EnemyPlacementContentDefinition?> enemyContent = content.Enemies ?? [];

        if (enemyContent.Count == 0)
        {
            issues.Add("at least one enemy placement is required.");
        }

        var occupiedPositions = new HashSet<GridPos> { playerStart };

        foreach (EnemyPlacementContentDefinition? placement in enemyContent)
        {
            if (placement == null)
            {
                issues.Add("enemies contains a null placement.");
                continue;
            }

            string enemyId = (placement.EnemyId ?? "").Trim();
            var position = new GridPos(placement.X, placement.Y);
            ValidateEnemyReference(enemyId, issues);

            if (!IsInside(position, content.GridWidth, content.GridHeight))
            {
                issues.Add($"enemy '{enemyId}' position {position} is outside the grid.");
            }

            if (!occupiedPositions.Add(position))
            {
                issues.Add($"enemy '{enemyId}' position {position} overlaps another actor.");
            }

            enemies.Add(new EncounterEnemyPlacement(enemyId, position));
        }

        var tilePositions = new HashSet<GridPos>();

        foreach (TilePlacementContentDefinition? placement in content.Tiles ?? [])
        {
            if (placement == null)
            {
                issues.Add("tiles contains a null placement.");
                continue;
            }

            var position = new GridPos(placement.X, placement.Y);

            if (!IsInside(position, content.GridWidth, content.GridHeight))
            {
                issues.Add($"tile position {position} is outside the grid.");
            }

            if (!tilePositions.Add(position))
            {
                issues.Add($"tile position {position} is defined more than once.");
            }

            if (!TryParseNamedTileState(placement.State, out TileState state))
            {
                issues.Add($"tile position {position} has invalid state '{placement.State}'.");
                continue;
            }

            if (state.IsBlocking() && occupiedPositions.Contains(position))
            {
                issues.Add($"blocking tile at {position} overlaps an actor.");
            }

            tiles.Add(new EncounterTilePlacement(position, state));
        }

        EncounterGenerationDefinition? generation = ResolveGeneration(content.Generation, issues);

        if (generation != null && generation.EnemyCount < enemies.Count)
        {
            issues.Add(
                $"generation enemyCount {generation.EnemyCount} cannot be smaller than " +
                $"the authored enemy roster ({enemies.Count}).");
        }
        else if (generation != null
            && generation.EnemyCount > enemies.Count
            && !enemies.Any(enemy =>
                enemy.EnemyId != victoryTargetEnemyId
                && ProceduralEncounterGenerator.IsRepeatableEnemy(enemy.EnemyId)))
        {
            issues.Add(
                "generation enemyCount exceeds the authored roster, but no non-objective, non-boss enemy " +
                "is available for expansion.");
        }

        if (victoryTargetEnemyId != null)
        {
            ValidateEnemyReference(victoryTargetEnemyId, issues);
            int targetPlacements = enemies.Count(enemy => enemy.EnemyId == victoryTargetEnemyId);

            if (targetPlacements != 1)
            {
                issues.Add(
                    $"victoryTargetEnemyId '{victoryTargetEnemyId}' must have exactly one authored placement; " +
                    $"found {targetPlacements}.");
            }
        }

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<EncounterDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new EncounterDefinition(
            content.Id.Trim(),
            (content.DisplayNameKey ?? "").Trim(),
            environmentId,
            content.GridWidth,
            content.GridHeight,
            playerStart,
            enemies,
            tiles,
            content.IsFinal,
            content.RewardAmount,
            (content.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray(),
            generation,
            victoryTargetEnemyId));
    }

    private static EncounterGenerationDefinition? ResolveGeneration(
        GenerationContentDefinition? content,
        List<string> issues)
    {
        if (content == null)
        {
            return null;
        }

        var terrainStates = new List<TileState>();

        foreach (string? stateText in content.TerrainStates ?? [])
        {
            if (!TryParseNamedTileState(stateText, out TileState state))
            {
                issues.Add($"generation terrain state '{stateText}' is invalid.");
                continue;
            }

            if (!state.IsBlocking())
            {
                issues.Add($"generation terrain state '{state}' must be blocking.");
                continue;
            }

            if (!terrainStates.Contains(state))
            {
                terrainStates.Add(state);
            }
        }

        var generation = new EncounterGenerationDefinition(
            content.Seed,
            content.Width,
            content.Height,
            content.RoomCount,
            content.EnemyCount,
            content.MinRoomWidth,
            content.MinRoomHeight,
            content.MaxRoomWidth,
            content.MaxRoomHeight,
            content.CorridorWidth,
            content.ExtraConnectionPercent,
            content.TerrainVariationPercent,
            terrainStates);

        issues.AddRange(ProceduralEncounterGenerator.Validate(generation));
        return generation;
    }

    private static void RequireStringKey(string? key, string field, List<string> issues)
    {
        if (!ContentRegistry.HasString(key?.Trim() ?? ""))
        {
            issues.Add($"{field} references missing string key '{key}'.");
        }
    }

    private static void ValidateEnvironmentReference(string environmentId, List<string> issues)
    {
        if (EnvironmentDefinitionCatalog.IsDisabled(environmentId))
        {
            issues.Add($"environmentId references disabled environment '{environmentId}'.");
        }
        else if (!EnvironmentDefinitionCatalog.TryGet(environmentId, out _))
        {
            issues.Add($"environmentId references missing environment '{environmentId}'.");
        }
    }

    private static void ValidateEnemyReference(string enemyId, List<string> issues)
    {
        if (EnemyConfigCatalog.IsDisabled(enemyId))
        {
            issues.Add($"enemy placement references disabled enemy '{enemyId}'.");
        }
        else if (!EnemyConfigCatalog.TryGet(enemyId, out _))
        {
            issues.Add($"enemy placement references missing enemy '{enemyId}'.");
        }
    }

    private static bool IsInside(GridPos position, int width, int height)
    {
        return position.X >= 0 && position.X < width && position.Y >= 0 && position.Y < height;
    }

    private static bool TryParseNamedTileState(string? value, out TileState state)
    {
        return Enum.TryParse(value ?? "", ignoreCase: true, out state)
            && Enum.IsDefined(state)
            && !int.TryParse(value, out _);
    }

    private sealed class EncounterContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<EncounterContentDefinition>? Encounters { get; set; } = [];
    }

    private sealed class EncounterContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public string? EnvironmentId { get; set; } = "";
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public GridPositionContentDefinition? PlayerStart { get; set; } = new();
        public List<EnemyPlacementContentDefinition?>? Enemies { get; set; } = [];
        public List<TilePlacementContentDefinition?>? Tiles { get; set; } = [];
        public bool IsFinal { get; set; }
        public int RewardAmount { get; set; }
        public List<string>? Tags { get; set; } = [];
        public GenerationContentDefinition? Generation { get; set; }
        public string? VictoryTargetEnemyId { get; set; }
    }

    private sealed class GridPositionContentDefinition
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private sealed class EnemyPlacementContentDefinition
    {
        public string? EnemyId { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
    }

    private sealed class TilePlacementContentDefinition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string? State { get; set; } = "";
    }

    private sealed class GenerationContentDefinition
    {
        public int Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int RoomCount { get; set; }
        public int EnemyCount { get; set; }
        public int MinRoomWidth { get; set; }
        public int MinRoomHeight { get; set; }
        public int MaxRoomWidth { get; set; }
        public int MaxRoomHeight { get; set; }
        public int CorridorWidth { get; set; } = 1;
        public int ExtraConnectionPercent { get; set; }
        public int TerrainVariationPercent { get; set; }
        public List<string?>? TerrainStates { get; set; } = [];
    }
}
