using Godot;
using TheThingImDoing.Core;
using TheThingImDoing.World;

namespace TheThingImDoing.Actors;

public partial class MovementComponent : Node
{
    public static bool TryMoveActor(TacticalGrid grid, ActorComponent actor, Direction direction)
    {
        GridPos destination = actor.GridPosition.Offset(direction);

        if (!grid.TryMoveActor(actor.ActorId, destination))
        {
            return false;
        }

        actor.SetGridPosition(destination);
        return true;
    }
}

