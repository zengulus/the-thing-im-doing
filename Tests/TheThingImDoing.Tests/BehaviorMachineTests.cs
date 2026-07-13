using System;
using System.Collections.Generic;
using TheThingImDoing.Actors;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
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
        Assert.Contains(primitive.Parameters ?? [], parameter => parameter.Name == "maximum" && !parameter.Required);

        Assert.True(BehaviorDefinitionCatalog.TryGet(
            "behavior.clause.add_lightning_charge",
            out BehaviorDefinition? chargeBehavior));
        Assert.Contains(chargeBehavior.Steps, step => step.Op == "counter.add" && step.Maximum == 3);

        Assert.True(BehaviorDefinitionCatalog.TryGet("behavior.effect.poison_turn_start", out BehaviorDefinition? behavior));
        Assert.Contains(behavior.Steps, step => step.Op == "counter.consume" && step.Target == "effect");

        Assert.True(EffectDefinitionCatalog.TryGet("effect.poison", out EffectDefinition? effect));
        Assert.Contains(effect.GetBehaviorIds(EffectTriggerIds.TurnStart), behaviorId => behaviorId == "behavior.effect.poison_turn_start");
    }

    [Fact]
    public void EnemyCatalog_LoadsExpandedRosterWithEnemyScopedBehaviorPrimitives()
    {
        string[] enemyIds =
        {
            "enemy.spore_cantor",
            "enemy.iron_pilgrim",
            "enemy.moss_chirurgeon",
            "enemy.obsidian_crown"
        };

        foreach (string enemyId in enemyIds)
        {
            Assert.True(EnemyConfigCatalog.TryGet(enemyId, out EnemyConfig? enemy));
            Assert.False(string.IsNullOrWhiteSpace(enemy.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(enemy.Purpose));
            Assert.True(BehaviorDefinitionCatalog.TryGet(enemy.BehaviorId, out BehaviorDefinition? behavior));

            foreach (BehaviorStepDefinition step in behavior.Steps)
            {
                Assert.True(BehaviorPrimitiveCatalog.TryGet(step.Op, out BehaviorPrimitiveDefinition? primitive));
                Assert.Contains("enemy", primitive.Scopes ?? []);
            }
        }

        Assert.Contains("capstone", EnemyConfigCatalog.Get("enemy.obsidian_crown").Tags);
    }

    [Fact]
    public void EnemyBehaviorContext_AllowsGlassHoundToMoveAndAttack()
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(4, 1));

        RunEnemyTurns(encounter, 1);

        Assert.Equal(new GridPos(3, 1), hound.Position);

        RunEnemyTurns(encounter, 2);

        Assert.Equal(4, encounter.Player.Health);
    }

    [Fact]
    public void EnemyMovement_UsesDeterministicShortestPathAroundWall()
    {
        var encounter = new TacticalEncounter(7, 5, new GridPos(1, 2));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(5, 2));
        encounter.Grid.SetTile(new GridPos(4, 2), TileState.Wall);
        encounter.TryDamageActor(hound.Id, 1);

        RunEnemyTurns(encounter, 2);

        Assert.Equal(new GridPos(4, 1), hound.Position);
    }

    [Fact]
    public void EnemyMovement_RoutesAroundStationaryActor()
    {
        var encounter = new TacticalEncounter(7, 5, new GridPos(1, 2));
        encounter.AddEnemy("enemy.spore_cantor", new GridPos(4, 2));
        EncounterActor hound = encounter.AddEnemy("enemy.glass_hound", new GridPos(5, 2));
        encounter.TryDamageActor(hound.Id, 1);

        RunEnemyTurns(encounter, 2);

        Assert.Equal(new GridPos(4, 1), hound.Position);
    }

    [Fact]
    public void SporeCantor_AdjacentTurnDamagesAndPoisonsPlayer()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        EncounterActor cantor = encounter.AddEnemy("enemy.spore_cantor", new GridPos(2, 1));

        RunEnemyTurns(encounter, 1);

        EffectInstance poison = Assert.Single(
            encounter.Player.Effects,
            effect => effect.EffectId == "effect.poison" && effect.OwnerActorId == cantor.Id);
        Assert.Equal(1, poison.Counters.Get("counter.stack"));
        Assert.Equal(3, encounter.Player.Health);
    }

    [Fact]
    public void MossChirurgeon_SecondTurnHealsNearestAlly()
    {
        var encounter = new TacticalEncounter(8, 3, new GridPos(0, 1));
        encounter.AddEnemy("enemy.moss_chirurgeon", new GridPos(5, 1));
        EncounterActor ally = encounter.AddEnemy("enemy.glass_hound", new GridPos(4, 1));
        encounter.TryDamageActor(ally.Id, 2);

        RunEnemyTurns(encounter, 2);

        Assert.Equal(4, ally.Health);
    }

    [Fact]
    public void MossChirurgeon_DoesNotHealDormantOffscreenAlly()
    {
        var encounter = new TacticalEncounter(32, 3, new GridPos(1, 1));
        EncounterActor moss = encounter.AddEnemy("enemy.moss_chirurgeon", new GridPos(10, 1));
        EncounterActor distant = encounter.AddEnemy("enemy.glass_hound", new GridPos(26, 1));
        encounter.TryDamageActor(distant.Id, 2);
        GridPos startingPosition = moss.Position;

        RunEnemyTurns(encounter, 2);

        Assert.Equal(2, distant.Health);
        Assert.Equal(startingPosition, moss.Position);
        Assert.False(encounter.IsEnemyEngaged(distant));
    }

    [Fact]
    public void MossChirurgeon_DoesNotHealAllyThroughWall()
    {
        var encounter = new TacticalEncounter(11, 3, new GridPos(1, 1));
        EncounterActor moss = encounter.AddEnemy("enemy.moss_chirurgeon", new GridPos(4, 1));
        EncounterActor occluded = encounter.AddEnemy("enemy.glass_hound", new GridPos(9, 1));
        encounter.TryDamageActor(occluded.Id, 2);
        encounter.Grid.SetTile(new GridPos(6, 1), TileState.Wall);
        GridPos startingPosition = moss.Position;

        RunEnemyTurns(encounter, 2);

        Assert.Equal(2, occluded.Health);
        Assert.Equal(startingPosition, moss.Position);
    }

    [Fact]
    public void MossChirurgeon_IgnoresNearerFullAllyAndHealsFartherWoundedAlly()
    {
        var encounter = new TacticalEncounter(11, 3, new GridPos(0, 1));
        EncounterActor moss = encounter.AddEnemy("enemy.moss_chirurgeon", new GridPos(3, 1));
        EncounterActor nearerFull = encounter.AddEnemy("enemy.glass_hound", new GridPos(4, 1));
        EncounterActor fartherWounded = encounter.AddEnemy("enemy.glass_hound", new GridPos(8, 1));
        encounter.TryDamageActor(fartherWounded.Id, 2);
        moss.Counters.Add("counter.ai.mending", 1);

        RunEnemyTurns(encounter, 1);

        Assert.Equal(nearerFull.MaxHealth, nearerFull.Health);
        Assert.Equal(fartherWounded.MaxHealth, fartherWounded.Health);
    }

    [Fact]
    public void IronPilgrim_ThirdTurnGainsWardInsteadOfAdvancing()
    {
        var encounter = new TacticalEncounter(10, 3, new GridPos(0, 1));
        EncounterActor pilgrim = encounter.AddEnemy("enemy.iron_pilgrim", new GridPos(9, 1));

        RunEnemyTurns(encounter, 2);

        Assert.Equal(new GridPos(7, 1), pilgrim.Position);
        Assert.Equal("gain 1 ward", encounter.GetEnemyIntent(pilgrim));

        RunEnemyTurns(encounter, 1);

        Assert.Equal(new GridPos(7, 1), pilgrim.Position);
        Assert.NotNull(pilgrim.FindEffect("effect.ward", pilgrim.Id));
        Assert.Equal(0, pilgrim.Counters.Get("counter.ai.guard"));
    }

    [Fact]
    public void ObsidianCrown_ThirdTurnDetonatesBrandsAndGainsWard()
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        EncounterActor crown = encounter.AddEnemy("enemy.obsidian_crown", new GridPos(5, 1));

        RunEnemyTurns(encounter, 2);

        Assert.True(encounter.HasTileCondition(encounter.Player.Position, "condition.marked", crown.Id));
        Assert.Equal(
            "detonate brands, gain 1 ward, then strike or advance",
            encounter.GetEnemyIntent(crown));

        RunEnemyTurns(encounter, 1);

        Assert.Equal(3, encounter.Player.Health);
        Assert.False(encounter.HasTileCondition(encounter.Player.Position, "condition.marked", crown.Id));
        Assert.NotNull(crown.FindEffect("effect.ward", crown.Id));
        Assert.Equal(0, crown.Counters.Get("counter.ai.ritual"));
    }

    [Fact]
    public void ObsidianCrown_ReadyAdjacentIntentDisclosesAndPerformsDetonationThenStrike()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        EncounterActor crown = encounter.AddEnemy("enemy.obsidian_crown", new GridPos(2, 1));
        crown.Counters.Add("counter.ai.ritual", 2);
        encounter.AddTileCondition(encounter.Player.Position, "condition.marked", crown.Id);

        Assert.Equal(
            "detonate brands, gain 1 ward, then strike or advance",
            encounter.GetEnemyIntent(crown));

        RunEnemyTurns(encounter, 1);

        Assert.Equal(1, encounter.Player.Health);
        Assert.Equal(new GridPos(2, 1), crown.Position);
        Assert.False(encounter.HasTileCondition(encounter.Player.Position, "condition.marked", crown.Id));
        Assert.Equal(1, crown.FindEffect("effect.ward", crown.Id)!.Counters.Get("counter.stack"));
        Assert.Equal(0, crown.Counters.Get("counter.ai.ritual"));
    }

    [Fact]
    public void AddLightningCharge_SelfFocusAddsExactlyOne()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(4, 1));

        WorkingResult result = ExecuteChargeWorking(encounter, encounter.Player.Position);

        Assert.True(result.Succeeded);
        Assert.True(result.ChangedWorld);
        Assert.Equal(1, encounter.Player.Counters.Get("counter.bonus.charge"));
        Assert.Equal(1, result.CounterGains["counter.bonus.charge"]);
    }

    [Fact]
    public void AddLightningCharge_EmptyFocusAddsNothing()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(4, 1));

        WorkingResult result = ExecuteChargeWorking(encounter, new GridPos(2, 1));

        Assert.True(result.Succeeded);
        Assert.False(result.ChangedWorld);
        Assert.Equal(0, encounter.Player.Counters.Get("counter.bonus.charge"));
        Assert.Empty(result.CounterGains);
    }

    [Fact]
    public void AddLightningCharge_EnemyFocusChargesOnlyThatEnemy()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1));

        WorkingResult result = ExecuteChargeWorking(encounter, enemy.Position);

        Assert.True(result.Succeeded);
        Assert.True(result.ChangedWorld);
        Assert.Equal(0, encounter.Player.Counters.Get("counter.bonus.charge"));
        Assert.Equal(1, enemy.Counters.Get("counter.bonus.charge"));
        Assert.Equal(1, result.CounterGains["counter.bonus.charge"]);
    }

    [Fact]
    public void AddLightningCharge_CannotExceedThree()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(4, 1));
        WorkingResult result = null!;

        for (int cast = 0; cast < 5; cast++)
        {
            result = ExecuteChargeWorking(encounter, encounter.Player.Position);
        }

        Assert.Equal(3, encounter.Player.Counters.Get("counter.bonus.charge"));
        Assert.False(result.ChangedWorld);
        Assert.Empty(result.CounterGains);
    }

    [Fact]
    public void LightningShield_AdjacentHitDealsOneAndConsumesOneCharge()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 4);
        encounter.Player.Counters.Add("counter.bonus.charge", 3);
        encounter.AttachEffectToActor(
            encounter.Player.Id,
            "effect.lightning_shield",
            encounter.Player.Id,
            stacks: 0);

        Assert.True(encounter.TryMoveActor(enemy, Direction.West));

        Assert.Equal(3, enemy.Health);
        Assert.Equal(2, encounter.Player.Counters.Get("counter.bonus.charge"));
        Assert.NotNull(encounter.Player.FindEffect("effect.lightning_shield", encounter.Player.Id));
    }

    [Theory]
    [InlineData("enemy.ash_scribe")]
    [InlineData("enemy.obsidian_crown")]
    public void EnemyDeath_ClearsOwnedTileMarksButPreservesOtherOwners(string enemyId)
    {
        var encounter = new TacticalEncounter(6, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddEnemy(enemyId, new GridPos(4, 1));
        var firstMark = new GridPos(2, 1);
        var secondMark = new GridPos(3, 1);
        encounter.AddTileCondition(firstMark, "condition.marked", enemy.Id);
        encounter.AddTileCondition(secondMark, "condition.marked", enemy.Id);
        encounter.AddTileCondition(firstMark, "condition.marked", encounter.Player.Id);

        Assert.True(encounter.TryDamageActor(enemy.Id, enemy.MaxHealth));

        Assert.False(enemy.IsAlive);
        Assert.DoesNotContain(encounter.TileConditions, condition => condition.OwnerActorId == enemy.Id);
        Assert.True(encounter.HasTileCondition(firstMark, "condition.marked", encounter.Player.Id));
    }

    [Fact]
    public void LethalAdjacentShield_StopsLaterShieldsFromSpendingOnCorpse()
    {
        var encounter = new TacticalEncounter(
            5,
            4,
            new GridPos(1, 2),
            "rule.brittle_stone",
            playerHealth: 1,
            playerMaxHealth: 1);
        EncounterActor firstShield = encounter.AddDummyEnemy(new GridPos(2, 1), health: 4);
        EncounterActor secondShield = encounter.AddDummyEnemy(new GridPos(3, 2), health: 4);

        foreach (EncounterActor enemy in new[] { firstShield, secondShield })
        {
            enemy.Counters.Add("counter.bonus.charge", 1);
            encounter.AttachEffectToActor(
                enemy.Id,
                "effect.lightning_shield",
                enemy.Id,
                stacks: 0);
        }

        Assert.True(encounter.TryMoveActor(encounter.Player, Direction.East));

        Assert.False(encounter.Player.IsAlive);
        Assert.Equal(0, firstShield.Counters.Get("counter.bonus.charge"));
        Assert.Null(firstShield.FindEffect("effect.lightning_shield", firstShield.Id));
        Assert.Equal(1, secondShield.Counters.Get("counter.bonus.charge"));
        Assert.NotNull(secondShield.FindEffect("effect.lightning_shield", secondShield.Id));
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
    public void HealAtom_IntMaxAmountClampsWithoutKillingOrOrphaningLivingActor()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(4, 1));
        encounter.Player.ApplyDamage(2);
        var behavior = new BehaviorDefinition(
            "test.behavior.max_heal",
            [Step(1, "heal.apply", target: "self", amount: int.MaxValue)]);

        BehaviorExecutionResult result = new BehaviorMachine().Execute(
            behavior,
            new BehaviorExecutionContext
            {
                Encounter = encounter,
                SpellWorld = new EncounterSpellWorld(encounter),
                Caster = encounter.Player,
                Trace = new OmenTrace()
            });

        Assert.True(result.ChangedWorld);
        Assert.True(encounter.Player.IsAlive);
        Assert.Equal(encounter.Player.MaxHealth, encounter.Player.Health);
        Assert.Same(encounter.Player, encounter.GetActorAt(encounter.Player.Position));
    }

    [Fact]
    public void EncounterActor_HealCannotResurrectDeadActorAfterGridRemoval()
    {
        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 2);

        Assert.True(encounter.TryDamageActor(enemy.Id, enemy.MaxHealth));
        enemy.Heal(int.MaxValue);

        Assert.False(enemy.IsAlive);
        Assert.Equal(0, enemy.Health);
        Assert.Null(encounter.GetActorAt(enemy.Position));
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

    [Fact]
    public void WardMaxStacks_AppliesToGenericEffectCounterMutation()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EffectInstance ward = encounter.AttachEffectToActor(
            encounter.Player.Id,
            "effect.ward",
            encounter.Player.Id,
            stacks: 1)!;

        EffectCommandResult increased = encounter.ResolveEffectCommand(
            new ModifyEffectCounterCommand(ward, "counter.stack", 10));
        EffectCommandResult capped = encounter.ResolveEffectCommand(
            new ModifyEffectCounterCommand(ward, "counter.stack", 1));

        Assert.True(increased.ChangedWorld);
        Assert.Equal(2, increased.CounterValue);
        Assert.False(capped.ChangedWorld);
        Assert.Equal(2, capped.CounterValue);
        Assert.Equal(2, ward.Counters.Get("counter.stack"));
    }

    [Fact]
    public void WorkingDamage_WhenWardPreventsIt_ReportsOnlyTheWardChangeAndTruthfulTrace()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(4, 1), health: 3);
        encounter.RemoveRelic("relic.patient_bell");
        encounter.AttachEffectToActor(encounter.Player.Id, "effect.ward", encounter.Player.Id, stacks: 1);
        Working working = CreateSelectedTargetDamageWorking();

        WorkingPreview preview = encounter.PreviewWorkingDetailed(working, encounter.Player.Position);

        Assert.True(preview.Result.Succeeded);
        Assert.True(preview.Result.ChangedWorld);
        Assert.Equal(encounter.Player.MaxHealth, preview.Encounter.Player.Health);
        Assert.Empty(preview.Encounter.Player.Effects);
        Assert.Single(encounter.Player.Effects);
        Assert.Contains(preview.Result.Trace.Events, traceEvent =>
            traceEvent.Text.Contains("prevented", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(preview.Result.Trace.Events, traceEvent =>
            traceEvent.Text.StartsWith($"Damaged actor {encounter.Player.Id}", StringComparison.Ordinal));

        WorkingResult cast = encounter.TryCastWorking(working, encounter.Player.Position);

        Assert.True(cast.Succeeded);
        Assert.True(cast.ChangedWorld);
        Assert.Equal(encounter.Player.MaxHealth, encounter.Player.Health);
        Assert.Empty(encounter.Player.Effects);
        Assert.Contains(cast.Trace.Events, traceEvent =>
            traceEvent.Text.Contains("prevented", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cast.Trace.Events, traceEvent =>
            traceEvent.Text.StartsWith($"Damaged actor {encounter.Player.Id}", StringComparison.Ordinal));
    }

    [Fact]
    public void RelicHook_AfterSpellResolvedRunsOnCloneForPreviewAndRealEncounterForCast()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);
        encounter.AddRelic("relic.patient_bell");
        Working working = WorkingSamples.CreateMarkOrDamage();

        WorkingResult preview = encounter.PreviewWorking(working, new GridPos(3, 1));

        Assert.True(preview.ChangedWorld);
        Assert.Equal(0, encounter.Player.Counters.Get("counter.bonus.focus"));

        WorkingResult cast = encounter.TryCastWorking(working, new GridPos(3, 1));

        Assert.True(cast.Succeeded);
        Assert.Equal(1, encounter.Player.Counters.Get("counter.bonus.focus"));
    }

    [Fact]
    public void WorkingJson_RoundTripsSemanticDataAndOptionalLayout()
    {
        Working working = WorkingSamples.CreateEmergencyWall();

        Working roundTripped = WorkingJson.FromJson(working.ToJson());

        Assert.Equal(working.Id, roundTripped.Id);
        Assert.Equal(working.DisplayNameKey, roundTripped.DisplayNameKey);
        Assert.Equal(working.EntryNodeId, roundTripped.EntryNodeId);
        Assert.Equal(working.Nodes.Count, roundTripped.Nodes.Count);
        Assert.Equal(working.Nodes[1].ClauseId, roundTripped.Nodes[1].ClauseId);
        Assert.Equal(working.GetNodeLayout(1), roundTripped.GetNodeLayout(1));
    }

    private static void RunEnemyTurns(TacticalEncounter encounter, int count)
    {
        for (int i = 0; i < count; i++)
        {
            encounter.WaitPlayerTurn();
            encounter.RunEnemyTurn();
        }
    }

    private static Working CreateSelectedTargetDamageWorking()
    {
        var working = new Working("working.test.damage_selected", "workings.mark_or_damage.name");
        var aim = new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 };
        var damage = new WorkingNode(2, "clause.damage_them");
        working.AddNode(aim);
        working.AddNode(damage);
        return working;
    }

    private static WorkingResult ExecuteChargeWorking(TacticalEncounter encounter, GridPos selectedTarget)
    {
        var working = new Working("working.test.charge", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.add_lightning_charge"));
        return new WorkingMachine().Execute(
            working,
            new EncounterSpellWorld(encounter),
            encounter.Player.Id,
            selectedTarget);
    }

    private static BehaviorStepDefinition Step(
        int id,
        string op,
        int? next = null,
        int? trueStep = null,
        int? falseStep = null,
        int? amount = null,
        int? maximum = null,
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
            maximum,
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
