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

    public bool HasLineOfSight(GridPos from, GridPos to)
    {
        if (!IsInside(from) || !IsInside(to))
        {
            return false;
        }

        int x = from.X;
        int y = from.Y;
        int deltaX = Math.Abs(to.X - from.X);
        int deltaY = Math.Abs(to.Y - from.Y);
        int stepX = from.X < to.X ? 1 : -1;
        int stepY = from.Y < to.Y ? 1 : -1;
        int traversedX = 0;
        int traversedY = 0;

        while (traversedX < deltaX || traversedY < deltaY)
        {
            int decision = (1 + 2 * traversedX) * deltaY
                - (1 + 2 * traversedY) * deltaX;

            if (decision == 0)
            {
                GridPos horizontalNeighbor = new(x + stepX, y);
                GridPos verticalNeighbor = new(x, y + stepY);

                if (GetTile(horizontalNeighbor).IsBlocking()
                    && GetTile(verticalNeighbor).IsBlocking())
                {
                    return false;
                }

                x += stepX;
                y += stepY;
                traversedX++;
                traversedY++;
            }
            else if (decision < 0)
            {
                x += stepX;
                traversedX++;
            }
            else
            {
                y += stepY;
                traversedY++;
            }

            var position = new GridPos(x, y);

            if (position != to && GetTile(position).IsBlocking())
            {
                return false;
            }
        }

        return true;
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

    public IReadOnlyDictionary<int, GridPos> GetActorPositions()
    {
        return _positionsByActor;
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

    public TacticalGrid Clone()
    {
        var clone = new TacticalGrid(Width, Height);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var position = new GridPos(x, y);
                clone.SetTile(position, GetTile(position));
            }
        }

        foreach ((int actorId, GridPos position) in _positionsByActor)
        {
            clone.TryAddActor(actorId, position);
        }

        return clone;
    }

    private void ThrowIfOutside(GridPos position)
    {
        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is outside the grid.");
        }
    }
}
