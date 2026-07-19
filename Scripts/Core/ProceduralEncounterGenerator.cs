using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.World;

namespace TheThingImDoing.Core;

public sealed record ProceduralRoom(int X, int Y, int Width, int Height)
{
    public GridPos Center => new(X + Width / 2, Y + Height / 2);
}

public sealed record EncounterLayout(
    int Width,
    int Height,
    GridPos PlayerStart,
    IReadOnlyList<EncounterEnemyPlacement> Enemies,
    IReadOnlyList<EncounterTilePlacement> Tiles,
    IReadOnlyList<ProceduralRoom> Rooms,
    int? Seed)
{
    public static EncounterLayout FromAuthored(EncounterDefinition definition)
    {
        return new EncounterLayout(
            definition.GridWidth,
            definition.GridHeight,
            definition.PlayerStart,
            definition.Enemies.ToArray(),
            definition.Tiles.ToArray(),
            [],
            null);
    }
}

public static class ProceduralEncounterGenerator
{
    public const int MinimumWidth = 32;
    public const int MinimumHeight = 24;
    public const int MaximumDimension = 64;
    public const int ObjectiveMinimumDistance = 18;
    public const int ObjectiveMaximumDistance = 34;

    public static IReadOnlyList<string> Validate(EncounterGenerationDefinition configuration)
    {
        var issues = new List<string>();

        if (configuration.Width < MinimumWidth || configuration.Width > MaximumDimension)
        {
            issues.Add($"generation width must be between {MinimumWidth} and {MaximumDimension}.");
        }

        if (configuration.Height < MinimumHeight || configuration.Height > MaximumDimension)
        {
            issues.Add($"generation height must be between {MinimumHeight} and {MaximumDimension}.");
        }

        if (configuration.RoomCount < 2 || configuration.RoomCount > 24)
        {
            issues.Add("generation roomCount must be between 2 and 24.");
        }

        if (configuration.EnemyCount < 1 || configuration.EnemyCount > 64)
        {
            issues.Add("generation enemyCount must be between 1 and 64.");
        }

        long guaranteedRemoteFloor = (long)(configuration.RoomCount - 1)
            * configuration.MinRoomWidth
            * configuration.MinRoomHeight;

        if (configuration.EnemyCount > guaranteedRemoteFloor)
        {
            issues.Add(
                "generation enemyCount exceeds the floor space guaranteed outside the player start room.");
        }

        if (configuration.MinRoomWidth < 3 || configuration.MinRoomHeight < 3)
        {
            issues.Add("generation minimum room dimensions must be at least 3.");
        }

        if (configuration.MaxRoomWidth < configuration.MinRoomWidth
            || configuration.MaxRoomHeight < configuration.MinRoomHeight)
        {
            issues.Add("generation maximum room dimensions cannot be smaller than the minimums.");
        }

        if (configuration.MaxRoomWidth > configuration.Width - 2
            || configuration.MaxRoomHeight > configuration.Height - 2)
        {
            issues.Add("generation maximum room dimensions must fit inside the outer boundary.");
        }

        if (configuration.CorridorWidth < 1 || configuration.CorridorWidth > 3)
        {
            issues.Add("generation corridorWidth must be between 1 and 3.");
        }

        if (configuration.ExtraConnectionPercent < 0 || configuration.ExtraConnectionPercent > 100)
        {
            issues.Add("generation extraConnectionPercent must be between 0 and 100.");
        }

        if (configuration.TerrainVariationPercent < 0 || configuration.TerrainVariationPercent > 100)
        {
            issues.Add("generation terrainVariationPercent must be between 0 and 100.");
        }

        if (configuration.TerrainStates.Count == 0)
        {
            issues.Add("generation terrainStates must contain at least one blocking tile state.");
        }
        else if (configuration.TerrainStates.Any(state => !state.IsBlocking()))
        {
            issues.Add("generation terrainStates can only contain blocking tile states.");
        }

        if (issues.Count == 0 && !TryChooseCellGrid(configuration, out _, out _))
        {
            issues.Add("generation rooms do not fit within the requested dimensions.");
        }

        return issues;
    }

    public static IReadOnlyList<string> Validate(EncounterDefinition template)
    {
        if (template.Generation == null)
        {
            return [$"Encounter template '{template.Id}' has no procedural generation configuration."];
        }

        var issues = Validate(template.Generation).ToList();

        if (template.Enemies.Count == 0)
        {
            issues.Add("Procedural encounter template has no enemy roster.");
        }
        else if (template.Generation.EnemyCount < template.Enemies.Count)
        {
            issues.Add("Procedural enemy count cannot omit authored enemy archetypes.");
        }
        else if (template.Generation.EnemyCount > template.Enemies.Count
            && !template.Enemies.Any(enemy =>
                enemy.EnemyId != template.VictoryTargetEnemyId
                && IsRepeatableEnemy(enemy.EnemyId)))
        {
            issues.Add(
                "Procedural enemy count exceeds the authored roster, but no non-objective, non-boss enemy " +
                "is available for expansion.");
        }

        if (template.VictoryTargetEnemyId != null)
        {
            int objectiveCount = template.Enemies.Count(enemy =>
                enemy.EnemyId == template.VictoryTargetEnemyId);

            if (objectiveCount != 1)
            {
                issues.Add(
                    $"Victory target '{template.VictoryTargetEnemyId}' must occur exactly once in the " +
                    $"authored roster; found {objectiveCount}.");
            }
        }

        return issues;
    }

    public static EncounterLayout Generate(EncounterDefinition template, int? seedOverride = null)
    {
        EncounterGenerationDefinition configuration = template.Generation
            ?? throw new ArgumentException(
                $"Encounter template '{template.Id}' has no procedural generation configuration.",
                nameof(template));
        IReadOnlyList<string> issues = Validate(template);

        if (issues.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", issues), nameof(template));
        }

        int seed = seedOverride ?? configuration.Seed;
        var random = new StableRandom(seed);
        var tiles = new TileState[configuration.Width, configuration.Height];

        Fill(tiles, configuration.TerrainStates[0]);
        IReadOnlyList<ProceduralRoom> rooms = CreateRooms(configuration, random);

        foreach (ProceduralRoom room in rooms)
        {
            CarveRoom(tiles, room);
        }

        HashSet<(int Left, int Right)> connections = ConnectRooms(tiles, rooms, configuration, random);
        AddExtraConnections(tiles, rooms, connections, configuration, random);
        ApplyTerrainVariation(tiles, configuration, random);

        GridPos playerStart = rooms[0].Center;
        IReadOnlyList<EncounterEnemyPlacement> enemyRoster = ExpandEnemyRoster(
            template.Enemies,
            configuration.EnemyCount,
            template.VictoryTargetEnemyId,
            random);
        IReadOnlyList<EncounterEnemyPlacement> enemies = PlaceEnemies(
            tiles,
            enemyRoster,
            rooms,
            playerStart,
            template.VictoryTargetEnemyId,
            random);
        IReadOnlyList<EncounterTilePlacement> placements = BuildTilePlacements(tiles);

        return new EncounterLayout(
            configuration.Width,
            configuration.Height,
            playerStart,
            enemies,
            placements,
            rooms,
            seed);
    }

    private static IReadOnlyList<ProceduralRoom> CreateRooms(
        EncounterGenerationDefinition configuration,
        StableRandom random)
    {
        if (!TryChooseCellGrid(configuration, out int columns, out int rows))
        {
            throw new InvalidOperationException("Validated procedural room grid could not be constructed.");
        }

        int interiorWidth = configuration.Width - 2;
        int interiorHeight = configuration.Height - 2;
        var rooms = new List<ProceduralRoom>(configuration.RoomCount);

        for (int index = 0; index < configuration.RoomCount; index++)
        {
            int column = index % columns;
            int row = index / columns;
            int cellLeft = 1 + column * interiorWidth / columns;
            int cellRightExclusive = 1 + (column + 1) * interiorWidth / columns;
            int cellTop = 1 + row * interiorHeight / rows;
            int cellBottomExclusive = 1 + (row + 1) * interiorHeight / rows;
            int cellWidth = cellRightExclusive - cellLeft;
            int cellHeight = cellBottomExclusive - cellTop;
            int horizontalPadding = cellWidth >= configuration.MinRoomWidth + 2 ? 1 : 0;
            int verticalPadding = cellHeight >= configuration.MinRoomHeight + 2 ? 1 : 0;
            int roomWidth = random.NextInclusive(
                configuration.MinRoomWidth,
                Math.Min(configuration.MaxRoomWidth, cellWidth - horizontalPadding * 2));
            int roomHeight = random.NextInclusive(
                configuration.MinRoomHeight,
                Math.Min(configuration.MaxRoomHeight, cellHeight - verticalPadding * 2));
            int x = random.NextInclusive(
                cellLeft + horizontalPadding,
                cellRightExclusive - horizontalPadding - roomWidth);
            int y = random.NextInclusive(
                cellTop + verticalPadding,
                cellBottomExclusive - verticalPadding - roomHeight);
            rooms.Add(new ProceduralRoom(x, y, roomWidth, roomHeight));
        }

        return rooms;
    }

    private static bool TryChooseCellGrid(
        EncounterGenerationDefinition configuration,
        out int selectedColumns,
        out int selectedRows)
    {
        selectedColumns = 0;
        selectedRows = 0;
        double targetAspect = configuration.Width / (double)configuration.Height;
        double bestScore = double.MaxValue;

        for (int columns = 1; columns <= configuration.RoomCount; columns++)
        {
            int rows = (configuration.RoomCount + columns - 1) / columns;
            int smallestCellWidth = (configuration.Width - 2) / columns;
            int smallestCellHeight = (configuration.Height - 2) / rows;

            if (smallestCellWidth < configuration.MinRoomWidth
                || smallestCellHeight < configuration.MinRoomHeight)
            {
                continue;
            }

            int unusedCells = columns * rows - configuration.RoomCount;
            double gridAspect = columns / (double)rows;
            double score = Math.Abs(gridAspect - targetAspect) + unusedCells * 0.08;

            if (score < bestScore)
            {
                bestScore = score;
                selectedColumns = columns;
                selectedRows = rows;
            }
        }

        return selectedColumns > 0;
    }

    private static HashSet<(int Left, int Right)> ConnectRooms(
        TileState[,] tiles,
        IReadOnlyList<ProceduralRoom> rooms,
        EncounterGenerationDefinition configuration,
        StableRandom random)
    {
        var connected = new HashSet<int> { 0 };
        var connections = new HashSet<(int Left, int Right)>();

        while (connected.Count < rooms.Count)
        {
            int bestLeft = -1;
            int bestRight = -1;
            int bestDistance = int.MaxValue;

            foreach (int left in connected.OrderBy(value => value))
            {
                for (int right = 0; right < rooms.Count; right++)
                {
                    if (connected.Contains(right))
                    {
                        continue;
                    }

                    int distance = rooms[left].Center.ManhattanDistanceTo(rooms[right].Center);

                    if (distance < bestDistance
                        || (distance == bestDistance && (left < bestLeft || (left == bestLeft && right < bestRight))))
                    {
                        bestLeft = left;
                        bestRight = right;
                        bestDistance = distance;
                    }
                }
            }

            CarveCorridor(
                tiles,
                rooms[bestLeft].Center,
                rooms[bestRight].Center,
                configuration.CorridorWidth,
                random.NextBool());
            connected.Add(bestRight);
            connections.Add(NormalizeConnection(bestLeft, bestRight));
        }

        return connections;
    }

    private static void AddExtraConnections(
        TileState[,] tiles,
        IReadOnlyList<ProceduralRoom> rooms,
        HashSet<(int Left, int Right)> connections,
        EncounterGenerationDefinition configuration,
        StableRandom random)
    {
        for (int left = 0; left < rooms.Count; left++)
        {
            for (int right = left + 1; right < rooms.Count; right++)
            {
                var connection = (left, right);

                if (connections.Contains(connection)
                    || !random.NextPercent(configuration.ExtraConnectionPercent))
                {
                    continue;
                }

                CarveCorridor(
                    tiles,
                    rooms[left].Center,
                    rooms[right].Center,
                    configuration.CorridorWidth,
                    random.NextBool());
                connections.Add(connection);
            }
        }
    }

    private static void CarveRoom(TileState[,] tiles, ProceduralRoom room)
    {
        for (int y = room.Y; y < room.Y + room.Height; y++)
        {
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                tiles[x, y] = TileState.Floor;
            }
        }
    }

    private static void CarveCorridor(
        TileState[,] tiles,
        GridPos from,
        GridPos to,
        int width,
        bool horizontalFirst)
    {
        if (horizontalFirst)
        {
            CarveHorizontal(tiles, from.X, to.X, from.Y, width);
            CarveVertical(tiles, from.Y, to.Y, to.X, width);
        }
        else
        {
            CarveVertical(tiles, from.Y, to.Y, from.X, width);
            CarveHorizontal(tiles, from.X, to.X, to.Y, width);
        }
    }

    private static void CarveHorizontal(TileState[,] tiles, int fromX, int toX, int y, int width)
    {
        int offsetStart = -(width - 1) / 2;

        for (int x = Math.Min(fromX, toX); x <= Math.Max(fromX, toX); x++)
        {
            for (int offset = 0; offset < width; offset++)
            {
                SetFloorIfInterior(tiles, x, y + offsetStart + offset);
            }
        }
    }

    private static void CarveVertical(TileState[,] tiles, int fromY, int toY, int x, int width)
    {
        int offsetStart = -(width - 1) / 2;

        for (int y = Math.Min(fromY, toY); y <= Math.Max(fromY, toY); y++)
        {
            for (int offset = 0; offset < width; offset++)
            {
                SetFloorIfInterior(tiles, x + offsetStart + offset, y);
            }
        }
    }

    private static void SetFloorIfInterior(TileState[,] tiles, int x, int y)
    {
        if (x > 0 && x < tiles.GetLength(0) - 1 && y > 0 && y < tiles.GetLength(1) - 1)
        {
            tiles[x, y] = TileState.Floor;
        }
    }

    private static void ApplyTerrainVariation(
        TileState[,] tiles,
        EncounterGenerationDefinition configuration,
        StableRandom random)
    {
        if (configuration.TerrainStates.Count < 2 || configuration.TerrainVariationPercent == 0)
        {
            return;
        }

        for (int y = 1; y < configuration.Height - 1; y++)
        {
            for (int x = 1; x < configuration.Width - 1; x++)
            {
                if (!tiles[x, y].IsBlocking()
                    || !random.NextPercent(configuration.TerrainVariationPercent))
                {
                    continue;
                }

                tiles[x, y] = configuration.TerrainStates[
                    random.Next(1, configuration.TerrainStates.Count)];
            }
        }
    }

    private static IReadOnlyList<EncounterEnemyPlacement> PlaceEnemies(
        TileState[,] tiles,
        IReadOnlyList<EncounterEnemyPlacement> enemyTemplates,
        IReadOnlyList<ProceduralRoom> rooms,
        GridPos playerStart,
        string? victoryTargetEnemyId,
        StableRandom random)
    {
        ProceduralRoom startRoom = rooms[0];
        var roomCandidates = rooms
            .Skip(1)
            .Select(room => room.Center)
            .ToList();
        var floorCandidates = GetFloorPositions(tiles)
            .Where(position => position != playerStart && !Contains(startRoom, position))
            .Distinct()
            .ToList();
        random.Shuffle(roomCandidates);
        random.Shuffle(floorCandidates);
        var occupied = new List<GridPos> { playerStart };
        var placements = new List<EncounterEnemyPlacement>(enemyTemplates.Count);

        foreach (EncounterEnemyPlacement template in enemyTemplates)
        {
            IList<GridPos> candidatePool = roomCandidates.Count > 0
                ? roomCandidates
                : floorCandidates;

            if (candidatePool.Count == 0)
            {
                throw new InvalidOperationException("Procedural layout has too few floor tiles for its actors.");
            }

            GridPos selected = template.EnemyId == victoryTargetEnemyId
                ? candidatePool
                    .OrderBy(position => DistanceOutsideObjectiveBand(
                        position.ManhattanDistanceTo(playerStart)))
                    .ThenByDescending(position => position.ManhattanDistanceTo(playerStart))
                    .First()
                : candidatePool
                    .OrderByDescending(position => occupied.Min(other => position.ManhattanDistanceTo(other)))
                    .ThenByDescending(position => position.ManhattanDistanceTo(playerStart))
                    .First();
            roomCandidates.Remove(selected);
            floorCandidates.Remove(selected);
            occupied.Add(selected);
            placements.Add(new EncounterEnemyPlacement(template.EnemyId, selected));
        }

        return placements;
    }

    private static int DistanceOutsideObjectiveBand(int distance)
    {
        if (distance < ObjectiveMinimumDistance)
        {
            return ObjectiveMinimumDistance - distance;
        }

        return distance > ObjectiveMaximumDistance
            ? distance - ObjectiveMaximumDistance
            : 0;
    }

    private static IReadOnlyList<EncounterEnemyPlacement> ExpandEnemyRoster(
        IReadOnlyList<EncounterEnemyPlacement> authoredRoster,
        int enemyCount,
        string? victoryTargetEnemyId,
        StableRandom random)
    {
        if (authoredRoster.Count == 0)
        {
            throw new InvalidOperationException("Procedural encounter template has no enemy roster.");
        }

        if (enemyCount < authoredRoster.Count)
        {
            throw new InvalidOperationException("Procedural enemy count cannot omit authored enemy archetypes.");
        }

        EncounterEnemyPlacement[] repeatableRoster = authoredRoster
            .Where(template =>
                template.EnemyId != victoryTargetEnemyId
                && IsRepeatableEnemy(template.EnemyId))
            .ToArray();

        if (enemyCount > authoredRoster.Count && repeatableRoster.Length == 0)
        {
            throw new InvalidOperationException(
                "Procedural enemy count cannot expand an objective-only or boss-only authored roster.");
        }

        var roster = authoredRoster.ToList();
        var cycle = new List<int>();

        while (roster.Count < enemyCount)
        {
            if (cycle.Count == 0)
            {
                cycle.AddRange(Enumerable.Range(0, repeatableRoster.Length));
                random.Shuffle(cycle);
            }

            int templateIndex = cycle[^1];
            cycle.RemoveAt(cycle.Count - 1);
            EncounterEnemyPlacement template = repeatableRoster[templateIndex];
            roster.Add(new EncounterEnemyPlacement(template.EnemyId, default));
        }

        return roster;
    }

    internal static bool IsRepeatableEnemy(string enemyId)
    {
        return EnemyConfigCatalog.TryGet(enemyId, out EnemyConfig? config)
            && !config.Tags.Contains("boss", StringComparer.Ordinal)
            && !config.Tags.Contains("capstone", StringComparer.Ordinal);
    }

    private static bool Contains(ProceduralRoom room, GridPos position)
    {
        return position.X >= room.X
            && position.X < room.X + room.Width
            && position.Y >= room.Y
            && position.Y < room.Y + room.Height;
    }

    private static IEnumerable<GridPos> GetFloorPositions(TileState[,] tiles)
    {
        for (int y = 0; y < tiles.GetLength(1); y++)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                if (tiles[x, y] == TileState.Floor)
                {
                    yield return new GridPos(x, y);
                }
            }
        }
    }

    private static IReadOnlyList<EncounterTilePlacement> BuildTilePlacements(TileState[,] tiles)
    {
        var placements = new List<EncounterTilePlacement>();

        for (int y = 0; y < tiles.GetLength(1); y++)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                TileState state = tiles[x, y];

                if (state != TileState.Floor)
                {
                    placements.Add(new EncounterTilePlacement(new GridPos(x, y), state));
                }
            }
        }

        return placements;
    }

    private static void Fill(TileState[,] tiles, TileState state)
    {
        for (int y = 0; y < tiles.GetLength(1); y++)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                tiles[x, y] = state;
            }
        }
    }

    private static (int Left, int Right) NormalizeConnection(int left, int right)
    {
        return left < right ? (left, right) : (right, left);
    }

    private sealed class StableRandom
    {
        private ulong _state;

        public StableRandom(int seed)
        {
            _state = unchecked((uint)seed) + 0x9E3779B97F4A7C15UL;
        }

        public int Next(int minimum, int maximumExclusive)
        {
            if (minimum >= maximumExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
            }

            return minimum + (int)(NextUInt64() % (uint)(maximumExclusive - minimum));
        }

        public int NextInclusive(int minimum, int maximum)
        {
            return minimum == maximum ? minimum : Next(minimum, checked(maximum + 1));
        }

        public bool NextBool()
        {
            return (NextUInt64() & 1UL) == 0UL;
        }

        public bool NextPercent(int percent)
        {
            return percent > 0 && (percent >= 100 || Next(0, 100) < percent);
        }

        public void Shuffle<T>(IList<T> values)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swapIndex = Next(0, index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }

        private ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong value = _state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
