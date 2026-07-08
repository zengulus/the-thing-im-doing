using Godot;
using TheThingImDoing.Core;
using TheThingImDoing.World;

namespace TheThingImDoing.Actors;

public partial class EnemyController : Node
{
    [Export] public NodePath ActorPath { get; set; } = new();

    private ActorComponent? _actor;

    public override void _Ready()
    {
        _actor = ActorPath.IsEmpty ? GetParent() as ActorComponent : GetNode<ActorComponent>(ActorPath);
    }

    public bool TakeTurn(TacticalGrid grid, ActorComponent target)
    {
        if (_actor == null)
        {
            return false;
        }

        if (_actor.GridPosition.ManhattanDistanceTo(target.GridPosition) <= 1)
        {
            return false;
        }

        Direction primaryDirection = GetPrimaryDirectionToward(_actor.GridPosition, target.GridPosition);
        return MovementComponent.TryMoveActor(grid, _actor, primaryDirection);
    }

    private static Direction GetPrimaryDirectionToward(GridPos from, GridPos to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            return dx >= 0 ? Direction.East : Direction.West;
        }

        return dy >= 0 ? Direction.South : Direction.North;
    }
}

