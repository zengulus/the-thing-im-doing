using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Spells;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class DirectDamageSetupContractTests
{
    private const string DamageOp = "damage.apply";
    private const string SetupEffect = "condition.marked";

    [Fact]
    public void EveryDirectDamageClause_RequiresAndConsumesCasterOwnedMarkBeforeDamage()
    {
        (ClauseDefinition Clause, BehaviorDefinition Behavior)[] directDamageClauses =
            ClauseDefinitionCatalog.All
                .Select(clause => (Clause: clause, Behavior: BehaviorDefinitionCatalog.Get(clause.BehaviorId)))
                .Where(pair => pair.Behavior.Steps.Any(step => step.Op == DamageOp))
                .ToArray();

        Assert.NotEmpty(directDamageClauses);

        foreach ((ClauseDefinition clause, BehaviorDefinition behavior) in directDamageClauses)
        {
            Assert.Equal(ClauseRole.Consumer, clause.Role);
            Assert.Contains("damage", clause.Tags ?? []);
            Assert.Empty(ClauseDefinitionCatalog.ValidateDirectDamageContract(
                behavior,
                clause.Role,
                clause.Tags ?? []));
            AssertEveryDamagePathConsumesSetup(clause, behavior);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void DirectDamageContract_RejectsNonPositiveCounterConsumption(int amount)
    {
        var behavior = new BehaviorDefinition(
            "behavior.test.invalid_counter_damage",
            [
                Step(
                    1,
                    "counter.consume",
                    trueStep: 2,
                    falseStep: 3,
                    amount: amount,
                    counter: "counter.bonus.focus",
                    target: "caster"),
                Step(2, DamageOp, amount: 1, target: "focus.actor"),
                Step(3, "flow.stop")
            ]);

        IReadOnlyList<string> issues = ClauseDefinitionCatalog.ValidateDirectDamageContract(
            behavior,
            ClauseRole.Consumer,
            ["damage"]);

        Assert.Contains(issues, issue => issue.Contains("every path to direct damage"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("focus.actor")]
    public void DirectDamageContract_RejectsOwnerlessOrMismatchedEffectConsumption(string owner)
    {
        var behavior = new BehaviorDefinition(
            "behavior.test.invalid_effect_damage",
            [
                Step(
                    1,
                    "effect.has",
                    trueStep: 2,
                    falseStep: 4,
                    effect: SetupEffect,
                    target: "focus",
                    source: "caster"),
                Step(
                    2,
                    "effect.detach",
                    next: 3,
                    effect: SetupEffect,
                    target: "focus.effect",
                    source: "focus.actor",
                    owner: owner),
                Step(3, DamageOp, amount: 1, target: "focus.actor"),
                Step(4, "flow.stop")
            ]);

        IReadOnlyList<string> issues = ClauseDefinitionCatalog.ValidateDirectDamageContract(
            behavior,
            ClauseRole.Consumer,
            ["damage"]);

        Assert.Contains(issues, issue => issue.Contains("every path to direct damage"));
    }

    [Fact]
    public void DirectDamageContract_AcceptsPositiveCounterConsumptionAsSetupAndPayoff()
    {
        var behavior = new BehaviorDefinition(
            "behavior.test.valid_counter_damage",
            [
                Step(
                    1,
                    "counter.consume",
                    trueStep: 2,
                    falseStep: 3,
                    amount: 1,
                    counter: "counter.bonus.focus",
                    target: "caster"),
                Step(2, DamageOp, amount: 1, target: "focus.actor"),
                Step(3, "flow.stop")
            ]);

        Assert.Empty(ClauseDefinitionCatalog.ValidateDirectDamageContract(
            behavior,
            ClauseRole.Consumer,
            ["damage"]));
    }

    private static void AssertEveryDamagePathConsumesSetup(
        ClauseDefinition clause,
        BehaviorDefinition behavior)
    {
        Dictionary<int, BehaviorStepDefinition> steps = behavior.Steps
            .ToDictionary(step => step.Id);
        int[] declaredDamageSteps = behavior.Steps
            .Where(step => step.Op == DamageOp)
            .Select(step => step.Id)
            .OrderBy(id => id)
            .ToArray();
        var reachableDamageSteps = new HashSet<int>();
        var visited = new HashSet<TraversalState>();
        var pending = new Stack<TraversalState>();
        pending.Push(new TraversalState(behavior.Steps[0].Id, SetupSeen: false, SetupConsumed: false));

        while (pending.TryPop(out TraversalState state))
        {
            if (!visited.Add(state))
            {
                continue;
            }

            BehaviorStepDefinition step = steps[state.StepId];
            bool setupSeen = state.SetupSeen;
            bool setupConsumed = state.SetupConsumed;

            if (step.Op == DamageOp)
            {
                reachableDamageSteps.Add(step.Id);
                Assert.True(
                    setupConsumed,
                    $"Clause '{clause.Id}' behavior '{behavior.Id}' can reach damage step " +
                    $"{step.Id} without first taking the true branch of a caster-owned " +
                    $"{SetupEffect} gate and consuming that setup.");
            }

            if (IsSetupGate(step))
            {
                Push(step.True ?? step.Next, setupSeen: true, setupConsumed);
                Push(step.False ?? step.Next, setupSeen, setupConsumed);
                continue;
            }

            if (IsSetupConsumption(step) && setupSeen)
            {
                setupConsumed = true;
            }

            if (step.Op == "flow.stop")
            {
                continue;
            }

            if (IsBranching(step.Op))
            {
                Push(step.True ?? step.Next, setupSeen, setupConsumed);
                Push(step.False ?? step.Next, setupSeen, setupConsumed);
                continue;
            }

            Push(step.Next ?? GetImplicitNext(behavior, step.Id), setupSeen, setupConsumed);
        }

        Assert.Equal(declaredDamageSteps, reachableDamageSteps.OrderBy(id => id));
        return;

        void Push(int? stepId, bool setupSeen, bool setupConsumed)
        {
            if (stepId.HasValue)
            {
                pending.Push(new TraversalState(stepId.Value, setupSeen, setupConsumed));
            }
        }
    }

    private static bool IsSetupGate(BehaviorStepDefinition step)
    {
        return step.Op == "effect.has"
            && step.Target == "focus"
            && step.Source == "caster"
            && step.Effect == SetupEffect;
    }

    private static bool IsSetupConsumption(BehaviorStepDefinition step)
    {
        return step.Op == "effect.detach"
            && step.Target == "focus.effect"
            && step.Source == "focus.actor"
            && step.Owner == "caster"
            && step.Effect == SetupEffect;
    }

    private static bool IsBranching(string op)
    {
        return op.StartsWith("branch.", StringComparison.Ordinal)
            || op is "counter.at_least" or "counter.consume" or "effect.has";
    }

    private static int? GetImplicitNext(BehaviorDefinition behavior, int currentStepId)
    {
        for (int index = 0; index < behavior.Steps.Count - 1; index++)
        {
            if (behavior.Steps[index].Id == currentStepId)
            {
                return behavior.Steps[index + 1].Id;
            }
        }

        return null;
    }

    private static BehaviorStepDefinition Step(
        int id,
        string op,
        int? next = null,
        int? trueStep = null,
        int? falseStep = null,
        int? amount = null,
        string counter = "",
        string effect = "",
        string target = "",
        string source = "",
        string owner = "")
    {
        return new BehaviorStepDefinition(
            id,
            op,
            next,
            trueStep,
            falseStep,
            amount,
            null,
            counter,
            effect,
            "",
            "",
            "",
            target,
            source,
            "",
            owner);
    }

    private readonly record struct TraversalState(
        int StepId,
        bool SetupSeen,
        bool SetupConsumed);
}
