using System;
using System.Collections.Generic;
using TheThingImDoing.Core;

namespace TheThingImDoing.World;

public sealed class TacticalGrid
{
    private readonly Dictionary<int, GridPos> _positionsByActor = new();
    private readonly Dictionary<GridPos, int> _actorsByPosition = new();
    private readonly TileState[,] _tiles;

    public TacticalGrid(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Grid height must be positive.");
        }

        Width = width;
        Height = height;
        _tiles = new TileState[width, height];
    }

    public int Width { get; }
    public int Height { get; }

    public bool IsInside(GridPos position)
    {
        return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
    }

    public TileState GetTile(GridPos position)
    {
        ThrowIfOutside(position);
        return _tiles[position.X, position.Y];
    }

    public void SetTile(GridPos position, TileState state)
    {
        ThrowIfOutside(position);
        _tiles[position.X, position.Y] = state;
    }

    public bool IsBlocked(GridPos position)
    {
        return !IsInside(position) || GetTile(position).IsBlocking();
    }

    public bool IsOccupied(GridPos position)
    {
        return _actorsByPosition.ContainsKey(position);
    }

    public bool IsEmpty(GridPos position)
    {
        return IsInside(position) && !IsBlocked(position) && !IsOccupied(position);
    }

    public TileOccupancy GetOccupancy(GridPos position)
    {
        return _actorsByPosition.TryGetValue(position, out int actorId)
            ? new TileOccupancy(actorId)
            : TileOccupancy.Empty;
    }

    public int? GetActorAt(GridPos position)
    {
        return _actorsByPosition.TryGetValue(position, out int actorId) ? actorId : null;
    }

    public GridPos? GetActorPosition(int actorId)
    {
        return _positionsByActor.TryGetValue(actorId, out GridPos position) ? position : null;
    }

    public bool TryAddActor(int actorId, GridPos position)
    {
        if (_positionsByActor.ContainsKey(actorId) || !IsEmpty(position))
        {
            return false;
        }

        _positionsByActor.Add(actorId, position);
        _actorsByPosition.Add(position, actorId);
        return true;
    }

    public bool TryMoveActor(int actorId, GridPos destination)
    {
        if (!_positionsByActor.TryGetValue(actorId, out GridPos currentPosition) || !IsEmpty(destination))
        {
            return false;
        }

        _actorsByPosition.Remove(currentPosition);
        _positionsByActor[actorId] = destination;
        _actorsByPosition[destination] = actorId;
        return true;
    }

    public bool RemoveActor(int actorId)
    {
        if (!_positionsByActor.Remove(actorId, out GridPos position))
        {
            return false;
        }

        _actorsByPosition.Remove(position);
        return true;
    }

    private void ThrowIfOutside(GridPos position)
    {
        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is outside the grid.");
        }
    }
}

