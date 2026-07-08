namespace TheThingImDoing.Core;

public enum Direction
{
    North,
    East,
    South,
    West
}

public static class DirectionExtensions
{
    public static GridPos ToOffset(this Direction direction)
    {
        return direction switch
        {
            Direction.North => new GridPos(0, -1),
            Direction.East => new GridPos(1, 0),
            Direction.South => new GridPos(0, 1),
            Direction.West => new GridPos(-1, 0),
            _ => GridPos.Zero
        };
    }

    public static Direction Opposite(this Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.South => Direction.North,
            Direction.West => Direction.East,
            _ => direction
        };
    }

    public static Direction TurnLeft(this Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.West,
            Direction.East => Direction.North,
            Direction.South => Direction.East,
            Direction.West => Direction.South,
            _ => direction
        };
    }

    public static Direction TurnRight(this Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            Direction.West => Direction.North,
            _ => direction
        };
    }
}

