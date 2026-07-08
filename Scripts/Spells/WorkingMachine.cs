using TheThingImDoing.Behaviors;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public sealed class WorkingMachine
{
    public WorkingResult Execute(Working working, ISpellWorld world, int casterActorId, GridPos selectedTarget)
    {
        var trace = new OmenTrace();
        EncounterActor? caster = world.GetActor(casterActorId);

        if (caster == null || !caster.IsAlive)
        {
            trace.Add("The working failed because the caster could not be found.");
            return WorkingResult.Failed(trace, "Missing caster.");
        }

        if (working.EntryNodeId == null)
        {
            trace.Add("The working has no entry node.");
            return WorkingResult.Failed(trace, "Missing entry node.");
        }

        var context = new WorkingContext
        {
            CasterActorId = casterActorId,
            SelectedTarget = selectedTarget,
            StepLimit = working.MaxSteps
        };

        int? currentNodeId = working.EntryNodeId;
        int steps = 0;
        bool changedWorld = false;

        while (currentNodeId.HasValue)
        {
            if (steps >= context.StepLimit)
            {
                trace.Add("Backlash: the working exceeded its step limit.");
                return WorkingResult.Failed(
                    trace,
                    "Step limit exceeded.",
                    changedWorld,
                    context.CounterCosts,
                    context.CounterGains);
            }

            WorkingNode? node = working.GetNode(currentNodeId.Value);

            if (node == null)
            {
                trace.Add($"Backlash: node {currentNodeId.Value} no longer exists.");
                return WorkingResult.Failed(
                    trace,
                    "Missing node.",
                    changedWorld,
                    context.CounterCosts,
                    context.CounterGains);
            }

            steps++;
            NodeExecutionResult result = ExecuteNode(node, context, world, caster, trace);
            changedWorld |= result.ChangedWorld;
            currentNodeId = result.NextNodeId;
        }

        trace.Add("The working ended.");
        return WorkingResult.Success(trace, changedWorld, context.CounterCosts, context.CounterGains);
    }

    private static NodeExecutionResult ExecuteNode(
        WorkingNode node,
        WorkingContext context,
        ISpellWorld world,
        EncounterActor caster,
        OmenTrace trace)
    {
        if (!ClauseDefinitionCatalog.TryGet(node.ClauseId, out ClauseDefinition? definition))
        {
            trace.Add($"Backlash: clause '{node.ClauseId}' is not registered.");
            return NodeExecutionResult.Next(node.NextNodeId);
        }

        foreach ((string counterId, int amount) in definition.CounterCosts)
        {
            caster.Counters.Add(counterId, -amount);
        }

        foreach ((string counterId, int amount) in definition.CounterGains)
        {
            caster.Counters.Add(counterId, amount);
        }

        context.RecordCounterChanges(definition);

        var behaviorResult = new BehaviorMachine().Execute(
            definition.BehaviorId,
            new BehaviorExecutionContext
            {
                SpellWorld = world,
                Working = context,
                Caster = caster,
                Trace = trace
            });

        int? nextNodeId = behaviorResult.Flow switch
        {
            BehaviorFlow.True => node.TrueNodeId,
            BehaviorFlow.False => node.FalseNodeId,
            _ => node.NextNodeId
        };

        return NodeExecutionResult.Next(nextNodeId, behaviorResult.ChangedWorld);
    }

    private readonly record struct NodeExecutionResult(int? NextNodeId, bool ChangedWorld)
    {
        public static NodeExecutionResult Next(int? nextNodeId, bool changedWorld = false)
        {
            return new NodeExecutionResult(nextNodeId, changedWorld);
        }
    }
}
