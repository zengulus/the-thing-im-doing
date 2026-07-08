using System.Collections.Generic;
using TheThingImDoing.Actors;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class BehaviorMachineTests
{
    [Fact]
    public void Execute_UsesInjectedAtomRegistryForBranching()
    {
        var calls = new List<string>();
        var registry = new BehaviorAtomRegistry(new Dictionary<string, BehaviorAtomExecutor>
        {
            ["test.branch_true"] = (_, _) => new BehaviorAtomResult(BehaviorFlow.True, false),
            ["test.true_path"] = (_, _) =>
            {
                calls.Add("true");
                return BehaviorAtomResult.Stop(changedWorld: true);
            },
            ["test.false_path"] = (_, _) =>
            {
                calls.Add("false");
                return BehaviorAtomResult.Stop(changedWorld: true);
            }
        });
        var behavior = new BehaviorDefinition(
            "test.behavior.branch",
            new[]
            {
                Step(1, "test.branch_true", trueStep: 2, falseStep: 3),
                Step(2, "test.true_path"),
                Step(3, "test.false_path")
            });

        BehaviorExecutionResult result = new BehaviorMachine(registry).Execute(
            behavior,
            new BehaviorExecutionContext { Trace = new OmenTrace() });

        Assert.True(result.ChangedWorld);
        Assert.Equal(new[] { "true" }, calls);
    }

    [Fact]
    public void Execute_BuiltInAtomsFocusSelectedTargetAndDamageActor()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);
        var working = new WorkingContext
        {
            CasterActorId = encounter.Player.Id,
            SelectedTarget = enemy.Position,
            StepLimit = 8
        };
        var behavior = new BehaviorDefinition(
            "test.behavior.damage_focus",
            new[]
            {
                Step(1, "focus.selected_target", next: 2),
                Step(2, "damage.apply", amount: 1, target: "focus.actor")
            });

        BehaviorExecutionResult result = new BehaviorMachine().Execute(
            behavior,
            new BehaviorExecutionContext
            {
                SpellWorld = new EncounterSpellWorld(encounter),
                Working = working,
                Caster = encounter.Player,
                Trace = new OmenTrace()
            });

        Assert.True(result.ChangedWorld);
        Assert.Equal(enemy.Id, working.FocusActorId);
        Assert.Equal(2, enemy.Health);
    }

    [Fact]
    public void Execute_BuiltInBranchAtomsRouteToFalsePathForEmptyFocus()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        var working = new WorkingContext
        {
            CasterActorId = encounter.Player.Id,
            SelectedTarget = new GridPos(3, 1),
            StepLimit = 8
        };
        var behavior = new BehaviorDefinition(
            "test.behavior.empty_focus_branch",
            new[]
            {
                Step(1, "focus.selected_target", next: 2),
                Step(2, "branch.occupied", target: "focus", trueStep: 3, falseStep: 4),
                Step(3, "counter.add", target: "caster", counter: "counter.test.true", amount: 1, next: 5),
                Step(4, "counter.add", target: "caster", counter: "counter.test.false", amount: 1, next: 5),
                Step(5, "flow.stop")
            });

        BehaviorExecutionResult result = new BehaviorMachine().Execute(
            behavior,
            new BehaviorExecutionContext
            {
                SpellWorld = new EncounterSpellWorld(encounter),
                Working = working,
                Caster = encounter.Player,
                Trace = new OmenTrace()
            });

        Assert.True(result.ChangedWorld);
        Assert.Equal(0, encounter.Player.Counters.Get("counter.test.true"));
        Assert.Equal(1, encounter.Player.Counters.Get("counter.test.false"));
    }

    [Fact]
    public void ContentCatalogs_LoadGenericPrimitiveAndEffectDefinitions()
    {
        Assert.True(BehaviorPrimitiveCatalog.TryGet("counter.add", out BehaviorPrimitiveDefinition? primitive));
        Assert.Contains(primitive.Parameters ?? [], parameter => parameter.Name == "target" && parameter.Required);

        Assert.True(BehaviorDefinitionCatalog.TryGet("behavior.effect.poison_turn_start", out BehaviorDefinition? behavior));
        Assert.Contains(behavior.Steps, step => step.Op == "counter.consume" && step.Target == "effect");

        Assert.True(EffectDefinitionCatalog.TryGet("effect.poison", out EffectDefinition? effect));
        Assert.Contains(effect.GetBehaviorIds(EffectTriggerIds.TurnStart), behaviorId => behaviorId == "behavior.effect.poison_turn_start");
    }

    [Fact]
    public void Execute_GenericEffectAttachMarksFocusedActor()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);
        var working = new WorkingContext
        {
            CasterActorId = encounter.Player.Id,
            SelectedTarget = enemy.Position,
            StepLimit = 8
        };
        var behavior = new BehaviorDefinition(
            "test.behavior.mark_focus",
            new[]
            {
                Step(1, "focus.selected_target", next: 2),
                Step(2, "effect.attach", target: "focus", source: "caster", effect: "condition.marked")
            });

        new BehaviorMachine().Execute(
            behavior,
            new BehaviorExecutionContext
            {
                SpellWorld = new EncounterSpellWorld(encounter),
                Working = working,
                Caster = encounter.Player,
                Trace = new OmenTrace()
            });

        Assert.True(encounter.HasActorCondition(enemy.Id, "condition.marked", encounter.Player.Id));
        Assert.Equal(0, enemy.Counters.Get("condition.marked.owner." + encounter.Player.Id));
    }

    [Fact]
    public void EffectTrigger_PoisonDamagesAndConsumesStackOnTurnStart()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);
        EffectInstance effect = encounter.AttachEffectToActor(enemy.Id, "effect.poison", encounter.Player.Id, stacks: 3)!;

        encounter.WaitPlayerTurn();
        encounter.RunEnemyTurn();

        Assert.Equal(2, enemy.Health);
        Assert.Equal(2, effect.Counters.Get("counter.stack"));
    }

    [Fact]
    public void EffectTrigger_WardPreventsDamageAndDetachesWhenSpent()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        encounter.AttachEffectToActor(encounter.Player.Id, "effect.ward", encounter.Player.Id, stacks: 1);

        bool changed = encounter.TryDamageActor(encounter.Player.Id, 2);

        Assert.False(changed);
        Assert.Equal(encounter.Player.MaxHealth, encounter.Player.Health);
        Assert.Empty(encounter.Player.Effects);
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
        string relation = "",
        string reference = "",
        string state = "",
        string target = "",
        string source = "",
        string mode = "")
    {
        return new BehaviorStepDefinition(
            id,
            op,
            next,
            trueStep,
            falseStep,
            amount,
            counter,
            effect,
            relation,
            reference,
            state,
            target,
            source,
            mode);
    }
}
