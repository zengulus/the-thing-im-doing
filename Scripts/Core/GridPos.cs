using System;
using Godot;

namespace TheThingImDoing.Core;

public readonly record struct GridPos(int X, int Y)
{
    public static GridPos Zero => new(0, 0);

    public static GridPos FromVector2I(Vector2I value)
    {
        return new GridPos(value.X, value.Y);
    }

    public Vector2I ToVector2I()
    {
        return new Vector2I(X, Y);
    }

    public Vector2 ToWorldPosition(int tileSize)
    {
        return new Vector2(X * tileSize, Y * tileSize);
    }

    public GridPos Offset(Direction direction, int distance = 1)
    {
        GridPos offset = direction.ToOffset();
        return new GridPos(X + offset.X * distance, Y + offset.Y * distance);
    }

    public int ManhattanDistanceTo(GridPos other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }

    public static GridPos operator +(GridPos left, GridPos right)
    {
        return new GridPos(left.X + right.X, left.Y + right.Y);
    }

    public static GridPos operator -(GridPos left, GridPos right)
    {
        return new GridPos(left.X - right.X, left.Y - right.Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
