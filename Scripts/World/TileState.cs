namespace TheThingImDoing.World;

public enum TileState
{
    Floor,
    Wall,
    RaisedStone
}

public static class TileStateExtensions
{
    public static bool IsBlocking(this TileState state)
    {
        return state is TileState.Wall or TileState.RaisedStone;
    }
}

