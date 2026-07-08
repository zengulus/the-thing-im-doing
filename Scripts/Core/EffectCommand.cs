using TheThingImDoing.World;

namespace TheThingImDoing.Core;

public abstract record EffectCommand;

public sealed record DamageActorCommand(int ActorId, int Amount, int? SourceActorId = null) : EffectCommand;

public sealed record PushActorCommand(int ActorId, Direction Direction, int Distance, int? SourceActorId = null)
    : EffectCommand;

public sealed record SetTileStateCommand(GridPos Position, TileState State) : EffectCommand;

public sealed record ModifyActorCounterCommand(
    int ActorId,
    string CounterId,
    int Amount,
    int? CasterActorId = null) : EffectCommand;

public sealed record ModifyTileCounterCommand(GridPos Position, string CounterId, int Amount) : EffectCommand;

public sealed record ModifyEffectCounterCommand(EffectInstance Effect, string CounterId, int Amount) : EffectCommand;

public sealed record AttachActorEffectCommand(
    int TargetActorId,
    string EffectId,
    int OwnerActorId,
    int Stacks = 0) : EffectCommand;

public sealed record AttachTileEffectCommand(
    GridPos Position,
    string EffectId,
    int OwnerActorId,
    int Stacks = 0) : EffectCommand;

public sealed record DetachActorEffectCommand(int TargetActorId, int InstanceId) : EffectCommand;

public sealed record RemoveTileEffectCommand(GridPos Position, int InstanceId) : EffectCommand;

public sealed record PreventEventDamageCommand : EffectCommand;

public readonly record struct EffectCommandResult(
    bool ChangedWorld,
    int CounterValue = 0,
    EffectInstance? Effect = null)
{
    public static EffectCommandResult NoChange { get; } = new(false);

    public static EffectCommandResult Changed(int counterValue = 0, EffectInstance? effect = null)
    {
        return new EffectCommandResult(true, counterValue, effect);
    }
}
