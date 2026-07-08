using Godot;
using TheThingImDoing.Core;

namespace TheThingImDoing.Actors;

public partial class ActorComponent : CharacterBody2D
{
    [Export] public int ActorId { get; set; } = -1;
    [Export] public Vector2I StartingGridPosition { get; set; } = Vector2I.Zero;
    [Export(PropertyHint.Range, "1,256,1")] public int TileSize { get; set; } = 32;

    public GridPos GridPosition { get; private set; }

    public override void _Ready()
    {
        SetGridPosition(GridPos.FromVector2I(StartingGridPosition));
    }

    public void SetGridPosition(GridPos position)
    {
        GridPosition = position;
        Position = position.ToWorldPosition(TileSize);
    }
}

