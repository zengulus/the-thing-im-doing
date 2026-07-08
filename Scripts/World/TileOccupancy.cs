namespace TheThingImDoing.World;

public readonly record struct TileOccupancy(int? ActorId)
{
    public static TileOccupancy Empty => new(null);

    public bool IsOccupied => ActorId.HasValue;
}

