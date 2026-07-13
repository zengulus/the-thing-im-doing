using System.Collections.Generic;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public sealed class WorkingMachine
{
    public WorkingResult Execute(Working working, ISpellWorld world, int casterActorId, GridPos selectedTarget)
    {
        var trace = new OmenTrace();
        IReadOnlyList<string> validationIssues = WorkingValidator.Validate(working);

        if (validationIssues.Count > 0)
        {
            trace.Add("Backlash: the working is invalid.");

            foreach (string issue in validationIssues)
            {
                trace.Add(issue);
            }

            return WorkingResult.Failed(trace, "Invalid working.");
        }

        EncounterActor? caster = world.GetActor(casterActorId);

        if (caster == null || !caster.IsAlive)
        {
            trace.Add("The working failed because the caster could not be found.");
            return WorkingResult.Failed(trace, "Missing caster.");
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
                    context.CounterGains,
                    context.CostAdjustment);
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
                    context.CounterGains,
                    context.CostAdjustment);
            }

            steps++;
            trace.EnterWorkingNode(node.Id);
            NodeExecutionResult result = ExecuteNode(node, context, world, caster, trace);
            trace.LeaveWorkingNode();
            changedWorld |= result.ChangedWorld;

            if (context.FailureReason != null)
            {
                return WorkingResult.Failed(
                    trace,
                    context.FailureReason,
                    changedWorld,
                    context.CounterCosts,
                    context.CounterGains,
                    context.CostAdjustment);
            }

            currentNodeId = result.NextNodeId;
        }

        trace.Add("The working ended.");
        return WorkingResult.Success(
            trace,
            changedWorld,
            context.CounterCosts,
            context.CounterGains,
            context.CostAdjustment);
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

        bool changedCounters = false;

        foreach ((string counterId, int amount) in definition.CounterCosts)
        {
            int before = caster.Counters.Get(counterId);
            int after = caster.Counters.Add(counterId, -amount);
            changedCounters |= after != before;
        }
        context.RecordCasterCounterCosts(definition, caster);

        foreach ((string counterId, int amount) in definition.CounterGains)
        {
            int before = caster.Counters.Get(counterId);
            int after = caster.Counters.Add(counterId, amount);
            changedCounters |= after != before;
        }
        context.RecordCasterCounterGains(definition, caster);

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

        return NodeExecutionResult.Next(nextNodeId, changedCounters || behaviorResult.ChangedWorld);
    }

    private readonly record struct NodeExecutionResult(int? NextNodeId, bool ChangedWorld)
    {
        public static NodeExecutionResult Next(int? nextNodeId, bool changedWorld = false)
        {
            return new NodeExecutionResult(nextNodeId, changedWorld);
        }
    }
}
