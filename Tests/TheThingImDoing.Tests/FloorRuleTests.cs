using System.Collections.Generic;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class FloorRuleTests
{
    [Fact]
    public void Catalog_LoadsDataDrivenRulesAndBehaviorGraphs()
    {
        var expectedHooks = new Dictionary<string, (string Trigger, string BehaviorId)>
        {
            ["rule.echoing_steps"] = (RuleTriggerIds.AfterMove, "behavior.rule.echoing_steps_after_move"),
            ["rule.verdant_pulse"] = (RuleTriggerIds.AfterSpellResolved, "behavior.rule.verdant_pulse_after_spell"),
            ["rule.obsidian_resonance"] = (RuleTriggerIds.AfterSpellResolved, "behavior.rule.obsidian_resonance_after_spell")
        };

        foreach ((string ruleId, (string trigger, string behaviorId)) in expectedHooks)
        {
            Assert.True(FloorRuleDefinitionCatalog.TryGet(ruleId, out FloorRuleDefinition? rule));
            Assert.False(string.IsNullOrWhiteSpace(rule.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(rule.Description));
            Assert.Contains(rule.Hooks, hook => hook.Trigger == trigger && hook.BehaviorId == behaviorId);
            Assert.True(BehaviorDefinitionCatalog.TryGet(behaviorId, out BehaviorDefinition? behavior));

            foreach (BehaviorStepDefinition step in behavior.Steps)
            {
                Assert.True(BehaviorPrimitiveCatalog.TryGet(step.Op, out _));
            }
        }

        Assert.Contains("capstone", FloorRuleDefinitionCatalog.Get("rule.obsidian_resonance").Tags);
    }

    [Fact]
    public void EchoingSteps_AfterMoveMarksEnteredTileForMover()
    {
        var encounter = CreateEncounter("rule.echoing_steps");
        GridPos destination = encounter.Player.Position.Offset(Direction.East);

        bool moved = encounter.TryMovePlayer(Direction.East);

        Assert.True(moved);
        Assert.True(encounter.HasTileCondition(destination, "condition.marked", encounter.Player.Id));
    }

    [Fact]
    public void EchoingSteps_PlayerOwnedTileMark_SatisfiesIfMarkedForItsOccupant()
    {
        TacticalEncounter encounter = CreateEncounter("rule.echoing_steps");
        encounter.RemoveRelic("relic.patient_bell");
        Assert.True(encounter.TryMoveActor(encounter.Player, Direction.East));
        GridPos markedPosition = encounter.Player.Position;

        WorkingPreview preview = encounter.PreviewWorkingDetailed(
            CreateIfMarkedDamageWorking(),
            markedPosition);

        Assert.True(encounter.HasTileCondition(markedPosition, "condition.marked", encounter.Player.Id));
        Assert.True(preview.Result.Succeeded);
        Assert.True(preview.Result.ChangedWorld);
        Assert.Equal(encounter.Player.Health - 1, preview.Encounter.Player.Health);
        Assert.Equal(encounter.Player.MaxHealth, encounter.Player.Health);
    }

    [Fact]
    public void EchoingSteps_ForeignOwnedTileMark_DoesNotSatisfyPlayersIfMarked()
    {
        TacticalEncounter encounter = CreateEncounter("rule.echoing_steps");
        encounter.RemoveRelic("relic.patient_bell");
        EncounterActor enemy = Assert.Single(encounter.Enemies);
        Assert.True(encounter.TryMoveActor(enemy, Direction.West));
        GridPos markedPosition = enemy.Position;

        WorkingPreview preview = encounter.PreviewWorkingDetailed(
            CreateIfMarkedDamageWorking(),
            markedPosition);

        Assert.True(encounter.HasTileCondition(markedPosition, "condition.marked", enemy.Id));
        Assert.False(encounter.HasTileCondition(markedPosition, "condition.marked", encounter.Player.Id));
        Assert.False(preview.Result.ChangedWorld);
        Assert.Equal(enemy.Health, preview.Encounter.GetActor(enemy.Id)!.Health);
    }

    [Fact]
    public void VerdantPulse_AfterSpellResolvedHealsPlayer()
    {
        var encounter = CreateEncounter("rule.verdant_pulse");
        encounter.TryDamageActor(encounter.Player.Id, 2);

        WorkingResult result = encounter.TryCastWorking(
            WorkingSamples.CreateMarkOrDamage(),
            new GridPos(4, 1));

        Assert.True(result.Succeeded);
        Assert.Equal(4, encounter.Player.Health);
    }

    [Fact]
    public void ObsidianResonance_FirstPulseHurtsThenTwoWardCanCoverHazardAndNextPulse()
    {
        var encounter = CreateEncounter("rule.obsidian_resonance");

        WorkingResult result = encounter.TryCastWorking(
            WorkingSamples.CreateMarkOrDamage(),
            new GridPos(4, 1));

        Assert.True(result.Succeeded);
        Assert.Equal(4, encounter.Player.Health);
        EffectInstance ward = Assert.IsType<EffectInstance>(
            encounter.Player.FindEffect("effect.ward", encounter.Player.Id));
        Assert.Equal(2, ward.Counters.Get("counter.stack"));

        // One external hazard spends only half the buffer.
        encounter.TryDamageActor(encounter.Player.Id, 1);
        Assert.Equal(1, ward.Counters.Get("counter.stack"));

        encounter.RunEnemyTurn();

        WorkingResult secondResult = encounter.TryCastWorking(
            WorkingSamples.CreateMarkOrDamage(),
            new GridPos(4, 1));

        Assert.True(secondResult.Succeeded);
        Assert.Equal(4, encounter.Player.Health);
        Assert.Equal(
            2,
            encounter.Player.FindEffect("effect.ward", encounter.Player.Id)!.Counters.Get("counter.stack"));
    }

    [Fact]
    public void SuccessfulAimOnlyNoOp_SpendsTurnWithoutTriggeringObsidianOrPatientBell()
    {
        var encounter = CreateEncounter("rule.obsidian_resonance");
        encounter.AddRelic("relic.patient_bell");

        WorkingResult result = encounter.TryCastWorking(CreateAimOnlyWorking(), new GridPos(4, 1));

        Assert.True(result.Succeeded);
        Assert.False(result.ChangedWorld);
        Assert.Equal(TurnPhase.EnemyTurn, encounter.Turns.Phase);
        Assert.Equal(encounter.Player.MaxHealth, encounter.Player.Health);
        Assert.Null(encounter.Player.FindEffect("effect.ward", encounter.Player.Id));
        Assert.Equal(0, encounter.Player.Counters.Get("counter.bonus.focus"));
    }

    [Fact]
    public void SuccessfulAimOnlyNoOp_DoesNotTriggerVerdantPulse()
    {
        var encounter = CreateEncounter("rule.verdant_pulse");
        encounter.TryDamageActor(encounter.Player.Id, 2);

        WorkingResult result = encounter.TryCastWorking(CreateAimOnlyWorking(), new GridPos(4, 1));

        Assert.True(result.Succeeded);
        Assert.False(result.ChangedWorld);
        Assert.Equal(3, encounter.Player.Health);
    }

    [Fact]
    public void ReapplyingExistingMark_IsNoOpAndDoesNotFarmAfterWorkingHooks()
    {
        var encounter = CreateEncounter("rule.verdant_pulse");
        encounter.AddRelic("relic.patient_bell");
        EncounterActor enemy = Assert.Single(encounter.Enemies);
        encounter.TryDamageActor(encounter.Player.Id, 2);
        Working mark = CreateMarkWorking();

        WorkingResult first = encounter.TryCastWorking(mark, enemy.Position);
        Assert.True(first.ChangedWorld);
        Assert.Equal(4, encounter.Player.Health);
        Assert.Equal(1, encounter.Player.Counters.Get("counter.bonus.focus"));
        encounter.RunEnemyTurn();

        WorkingResult repeated = encounter.TryCastWorking(mark, enemy.Position);

        Assert.True(repeated.Succeeded);
        Assert.False(repeated.ChangedWorld);
        Assert.Equal(4, encounter.Player.Health);
        Assert.Equal(1, encounter.Player.Counters.Get("counter.bonus.focus"));
    }

    [Fact]
    public void RepeatedObsidianResonance_CannotRaiseWardAboveConfiguredCap()
    {
        var encounter = new TacticalEncounter(
            8,
            3,
            new GridPos(1, 1),
            "rule.obsidian_resonance",
            playerHealth: 5,
            playerMaxHealth: 5);
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(6, 1), health: 20);
        Working damage = CreateSelectedDamageWorking();

        for (int castIndex = 0; castIndex < 5; castIndex++)
        {
            WorkingResult result = encounter.TryCastWorking(damage, enemy.Position);
            Assert.True(result.Succeeded);
            Assert.True(result.ChangedWorld);
            Assert.InRange(
                encounter.Player.FindEffect("effect.ward", encounter.Player.Id)!.Counters.Get("counter.stack"),
                1,
                2);
            encounter.Turns.EndEnemyTurn();
        }

        Assert.Equal(4, encounter.Player.Health);
        Assert.Equal(2, EffectDefinitionCatalog.Get("effect.ward").MaxStacks);
        Assert.Equal(
            2,
            encounter.Player.FindEffect("effect.ward", encounter.Player.Id)!.Counters.Get("counter.stack"));
    }

    [Fact]
    public void OrdinaryFloor_BlockedPushIsTruthfulNoOpWithoutCollisionDamage()
    {
        var encounter = new TacticalEncounter(
            6,
            3,
            new GridPos(1, 1),
            "rule.echoing_steps",
            playerHealth: 5,
            playerMaxHealth: 5);
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(2, 1), health: 4);
        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Wall);
        Working push = CreateSelectedPushWorking();

        WorkingPreview preview = encounter.PreviewWorkingDetailed(push, enemy.Position);

        Assert.True(preview.Result.Succeeded);
        Assert.False(preview.Result.ChangedWorld);
        Assert.Equal(4, preview.Encounter.GetActor(enemy.Id)!.Health);
        Assert.Equal(enemy.Position, preview.Encounter.GetActor(enemy.Id)!.Position);

        WorkingResult cast = encounter.TryCastWorking(push, enemy.Position);

        Assert.True(cast.Succeeded);
        Assert.False(cast.ChangedWorld);
        Assert.Equal(4, enemy.Health);
        Assert.Equal(new GridPos(2, 1), enemy.Position);
        Assert.Contains(cast.Trace.Events, item => item.Text.Contains("Forced movement") && item.Text.Contains("failed"));
    }

    [Fact]
    public void BrittleStone_PushCollisionBreaksStoneDamagesOnceAndReportsChange()
    {
        var encounter = new TacticalEncounter(
            6,
            3,
            new GridPos(1, 1),
            "rule.brittle_stone",
            playerHealth: 5,
            playerMaxHealth: 5);
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(2, 1), health: 4);
        var stone = new GridPos(3, 1);
        encounter.Grid.SetTile(stone, TileState.RaisedStone);
        Working push = CreateSelectedPushWorking();

        WorkingPreview preview = encounter.PreviewWorkingDetailed(push, enemy.Position);

        Assert.True(preview.Result.Succeeded);
        Assert.True(preview.Result.ChangedWorld);
        Assert.Equal(3, preview.Encounter.GetActor(enemy.Id)!.Health);
        Assert.Equal(TileState.Floor, preview.Encounter.Grid.GetTile(stone));
        Assert.Equal(4, enemy.Health);
        Assert.Equal(TileState.RaisedStone, encounter.Grid.GetTile(stone));

        WorkingResult cast = encounter.TryCastWorking(push, enemy.Position);

        Assert.True(cast.Succeeded);
        Assert.True(cast.ChangedWorld);
        Assert.Equal(3, enemy.Health);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(stone));
        Assert.Contains(cast.Trace.Events, item => item.Text == $"Set tile {stone} to Floor.");
        Assert.Contains(cast.Trace.Events, item => item.Text == $"Damaged actor {enemy.Id} for 1.");
        Assert.Contains(cast.Trace.Events, item => item.Text.Contains("Forced movement affected actor"));
        Assert.DoesNotContain(cast.Trace.Events, item => item.Text.StartsWith("Moved actor"));
    }

    [Fact]
    public void BrittleStone_PushIntoOccupiedFloorIsNoOpWithoutCollisionDamage()
    {
        var encounter = new TacticalEncounter(
            7,
            3,
            new GridPos(1, 1),
            "rule.brittle_stone",
            playerHealth: 5,
            playerMaxHealth: 5);
        EncounterActor pushed = encounter.AddDummyEnemy(new GridPos(2, 1), health: 4);
        EncounterActor blocker = encounter.AddDummyEnemy(new GridPos(3, 1), health: 4);
        Working push = CreateSelectedPushWorking();

        WorkingPreview preview = encounter.PreviewWorkingDetailed(push, pushed.Position);

        Assert.True(preview.Result.Succeeded);
        Assert.False(preview.Result.ChangedWorld);
        Assert.Equal(4, preview.Encounter.GetActor(pushed.Id)!.Health);
        Assert.Equal(4, preview.Encounter.GetActor(blocker.Id)!.Health);

        WorkingResult cast = encounter.TryCastWorking(push, pushed.Position);

        Assert.True(cast.Succeeded);
        Assert.False(cast.ChangedWorld);
        Assert.Equal(4, pushed.Health);
        Assert.Equal(4, blocker.Health);
        Assert.Equal(new GridPos(2, 1), pushed.Position);
        Assert.Equal(new GridPos(3, 1), blocker.Position);
    }

    [Fact]
    public void MoveEffectDeath_StopsBeforeAdjacencyAndAfterMoveHooks()
    {
        var encounter = new TacticalEncounter(
            6,
            3,
            new GridPos(1, 1),
            "rule.echoing_steps",
            playerHealth: 5,
            playerMaxHealth: 5);
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 2);
        encounter.AttachEffectToActor(enemy.Id, "effect.bleed", encounter.Player.Id, stacks: 2);
        encounter.Player.Counters.Add("counter.bonus.charge", 1);
        encounter.AttachEffectToActor(
            encounter.Player.Id,
            "effect.lightning_shield",
            encounter.Player.Id,
            stacks: 0);
        var destination = new GridPos(2, 1);

        Assert.True(encounter.TryMoveActor(enemy, Direction.West));

        Assert.False(enemy.IsAlive);
        Assert.Null(encounter.GetActorAt(destination));
        Assert.False(encounter.HasTileCondition(destination, "condition.marked", enemy.Id));
        Assert.Equal(1, encounter.Player.Counters.Get("counter.bonus.charge"));
        Assert.NotNull(encounter.Player.FindEffect("effect.lightning_shield", encounter.Player.Id));
    }

    private static TacticalEncounter CreateEncounter(string ruleId)
    {
        var encounter = new TacticalEncounter(
            6,
            3,
            new GridPos(1, 1),
            ruleId,
            playerHealth: 5,
            playerMaxHealth: 5);
        encounter.AddEnemy("enemy.glass_hound", new GridPos(4, 1));
        return encounter;
    }

    private static Working CreateIfMarkedDamageWorking()
    {
        var working = new Working("working.test.if_marked_damage", "workings.mark_or_damage.name");
        var aim = new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 };
        var condition = new WorkingNode(2, "clause.if_marked") { TrueNodeId = 3 };
        var damage = new WorkingNode(3, "clause.damage_them");
        working.AddNode(aim);
        working.AddNode(condition);
        working.AddNode(damage);
        return working;
    }

    private static Working CreateAimOnlyWorking()
    {
        var working = new Working("working.test.aim_only", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target"));
        return working;
    }

    private static Working CreateMarkWorking()
    {
        var working = new Working("working.test.mark", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.mark_them"));
        return working;
    }

    private static Working CreateSelectedDamageWorking()
    {
        var working = new Working("working.test.selected_damage", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.damage_them"));
        return working;
    }

    private static Working CreateSelectedPushWorking()
    {
        var working = new Working("working.test.selected_push", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.push_them"));
        return working;
    }
}
