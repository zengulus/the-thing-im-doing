using Godot;
using TheThingImDoing.Core;
using TheThingImDoing.World;

namespace TheThingImDoing.Actors;

public partial class PlayerController : Node
{
    [Export] public NodePath ActorPath { get; set; } = new();

    private ActorComponent? _actor;

    public override void _Ready()
    {
        _actor = ActorPath.IsEmpty ? GetParent() as ActorComponent : GetNode<ActorComponent>(ActorPath);
    }

    public bool TryMove(TacticalGrid grid, Direction direction)
    {
        return _actor != null && MovementComponent.TryMoveActor(grid, _actor, direction);
    }
}

