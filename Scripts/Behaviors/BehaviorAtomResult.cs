namespace TheThingImDoing.Behaviors;

public readonly record struct BehaviorAtomResult(BehaviorFlow Flow, bool ChangedWorld)
{
    public static BehaviorAtomResult Next(bool changedWorld = false)
    {
        return new BehaviorAtomResult(BehaviorFlow.Next, changedWorld);
    }

    public static BehaviorAtomResult Stop(bool changedWorld = false)
    {
        return new BehaviorAtomResult(BehaviorFlow.Stop, changedWorld);
    }
}
