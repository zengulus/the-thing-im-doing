namespace TheThingImDoing.Behaviors;

public readonly record struct BehaviorExecutionResult(BehaviorFlow Flow, bool ChangedWorld)
{
    public static BehaviorExecutionResult Next(bool changedWorld = false)
    {
        return new BehaviorExecutionResult(BehaviorFlow.Next, changedWorld);
    }
}

