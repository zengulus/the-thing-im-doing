using System;
using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Behaviors;

public delegate BehaviorAtomResult BehaviorAtomExecutor(
    BehaviorStepDefinition step,
    BehaviorExecutionContext context);

public sealed class BehaviorAtomRegistry
{
    private readonly IReadOnlyDictionary<string, BehaviorAtomExecutor> _executorsById;

    public BehaviorAtomRegistry(IEnumerable<KeyValuePair<string, BehaviorAtomExecutor>> executors)
    {
        _executorsById = executors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
    }

    public bool TryExecute(
        BehaviorStepDefinition step,
        BehaviorExecutionContext context,
        out BehaviorAtomResult result)
    {
        if (_executorsById.TryGetValue(step.Op, out BehaviorAtomExecutor? executor))
        {
            result = executor(step, context);
            return true;
        }

        result = BehaviorAtomResult.Next();
        return false;
    }
}
