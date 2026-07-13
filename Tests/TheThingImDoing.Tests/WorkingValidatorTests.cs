using System.Collections.Generic;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class WorkingValidatorTests
{
    [Fact]
    public void Validate_AcceptsKnownAcyclicWorking()
    {
        Working working = WorkingSamples.CreateMarkOrDamage();

        Assert.Empty(WorkingValidator.Validate(working));
    }

    [Fact]
    public void Validate_ReportsUnsupportedSchemaAndInvalidMaxSteps()
    {
        Working working = SingleNodeWorking();
        working.SchemaVersion = Working.CurrentSchemaVersion + 1;
        working.MaxSteps = 0;

        var issues = WorkingValidator.Validate(working);

        Assert.Contains(issues, issue => issue.Contains("Unsupported schema version"));
        Assert.Contains(issues, issue => issue.Contains("Max steps must be greater than zero"));
    }

    [Fact]
    public void Validate_ReportsMissingAndInvalidEntryNodes()
    {
        Working missingEntry = SingleNodeWorking();
        missingEntry.EntryNodeId = null;
        Working invalidEntry = SingleNodeWorking();
        invalidEntry.EntryNodeId = 99;

        Assert.Contains(
            WorkingValidator.Validate(missingEntry),
            issue => issue.Contains("no entry node"));
        Assert.Contains(
            WorkingValidator.Validate(invalidEntry),
            issue => issue.Contains("Entry node 99 does not exist"));
    }

    [Fact]
    public void Validate_ReportsUnknownClausesAndMissingReferences()
    {
        var working = new Working("working.invalid", "working.invalid");
        var node = new WorkingNode(1, "clause.not_registered") { NextNodeId = 99 };
        working.AddNode(node);

        var issues = WorkingValidator.Validate(working);

        Assert.Contains(issues, issue => issue.Contains("unknown clause 'clause.not_registered'"));
        Assert.Contains(issues, issue => issue.Contains("references missing node 99"));
    }

    [Fact]
    public void Validate_ReportsOutputsThatDoNotMatchClauseShape()
    {
        var working = new Working("working.invalid_ports", "working.invalid_ports");
        var ordinary = new WorkingNode(1, "clause.aim_at_target")
        {
            NextNodeId = 2,
            TrueNodeId = 2,
            FalseNodeId = 2
        };
        var condition = new WorkingNode(2, "clause.if_clear") { NextNodeId = 3 };
        var end = new WorkingNode(3, "clause.raise_stone");
        working.AddNode(ordinary);
        working.AddNode(condition);
        working.AddNode(end);

        var issues = WorkingValidator.Validate(working);

        Assert.Contains(issues, issue => issue.Contains("ordinary clause") && issue.Contains("True output"));
        Assert.Contains(issues, issue => issue.Contains("ordinary clause") && issue.Contains("False output"));
        Assert.Contains(issues, issue => issue.Contains("condition clause") && issue.Contains("Next output"));
    }

    [Fact]
    public void Validate_ReportsReachableCyclesAndDisconnectedNodesWithoutCallingTheirCyclesReachable()
    {
        var working = new Working("working.cycle", "working.cycle");
        var entry = new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 };
        var reachable = new WorkingNode(2, "clause.damage_them") { NextNodeId = 1 };
        var unreachable = new WorkingNode(3, "clause.aim_at_target") { NextNodeId = 3 };
        working.AddNode(entry);
        working.AddNode(reachable);
        working.AddNode(unreachable);

        var issues = WorkingValidator.Validate(working);

        string cycle = Assert.Single(issues, issue => issue.Contains("Reachable cycle"));
        Assert.Contains("1 -> 2 -> 1", cycle);
        Assert.DoesNotContain("3 -> 3", cycle);
        Assert.Contains(issues, issue => issue.Contains("Node 3") && issue.Contains("disconnected"));
    }

    [Fact]
    public void Validate_RejectsOrphanNodeDisconnectedFromEntry()
    {
        var working = new Working("working.orphan", "working.orphan");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target"));
        working.AddNode(new WorkingNode(2, "clause.damage_them"));

        IReadOnlyList<string> issues = WorkingValidator.Validate(working);

        Assert.Contains(issues, issue => issue.Contains("Node 2") && issue.Contains("disconnected"));
    }

    [Fact]
    public void Validate_TreatsBothConditionBranchesAsReachable()
    {
        var working = new Working("working.branches", "working.branches");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.if_clear") { TrueNodeId = 3, FalseNodeId = 4 });
        working.AddNode(new WorkingNode(3, "clause.damage_them"));
        working.AddNode(new WorkingNode(4, "clause.raise_stone"));

        Assert.Empty(WorkingValidator.Validate(working));
    }

    [Fact]
    public void UsesSelectedTarget_FindsLaterAndBranchedReachableClausesButNotOrphans()
    {
        var later = new Working("working.later_target", "working.later_target");
        later.AddNode(new WorkingNode(1, "clause.damage_them") { NextNodeId = 2 });
        later.AddNode(new WorkingNode(2, "clause.aim_at_target"));

        var trueBranch = new Working("working.true_target", "working.true_target");
        trueBranch.AddNode(new WorkingNode(1, "clause.if_clear") { TrueNodeId = 2, FalseNodeId = 3 });
        trueBranch.AddNode(new WorkingNode(2, "clause.aim_at_target"));
        trueBranch.AddNode(new WorkingNode(3, "clause.damage_them"));

        var falseBranch = new Working("working.false_target", "working.false_target");
        falseBranch.AddNode(new WorkingNode(1, "clause.if_clear") { TrueNodeId = 2, FalseNodeId = 3 });
        falseBranch.AddNode(new WorkingNode(2, "clause.damage_them"));
        falseBranch.AddNode(new WorkingNode(3, "clause.aim_at_target"));

        var orphan = new Working("working.orphan_target", "working.orphan_target");
        orphan.AddNode(new WorkingNode(1, "clause.damage_them"));
        orphan.AddNode(new WorkingNode(2, "clause.aim_at_target"));

        Assert.True(WorkingValidator.UsesSelectedTarget(later));
        Assert.True(WorkingValidator.UsesSelectedTarget(trueBranch));
        Assert.True(WorkingValidator.UsesSelectedTarget(falseBranch));
        Assert.False(WorkingValidator.UsesSelectedTarget(orphan));
    }

    [Fact]
    public void UsesNearestFoeTarget_RequiresReachableNearestFoeClause()
    {
        var nearest = new Working("working.nearest", "working.nearest");
        nearest.AddNode(new WorkingNode(1, "clause.if_clear") { TrueNodeId = 2, FalseNodeId = 3 });
        nearest.AddNode(new WorkingNode(2, "clause.aim_at_nearest_foe"));
        nearest.AddNode(new WorkingNode(3, "clause.damage_them"));

        var recallOnly = new Working("working.recall", "working.recall");
        recallOnly.AddNode(new WorkingNode(1, "clause.focus_memory_ref"));

        var orphanNearest = new Working("working.orphan_nearest", "working.orphan_nearest");
        orphanNearest.AddNode(new WorkingNode(1, "clause.focus_memory_ref"));
        orphanNearest.AddNode(new WorkingNode(2, "clause.aim_at_nearest_foe"));

        Assert.True(WorkingValidator.UsesNearestFoeTarget(nearest));
        Assert.False(WorkingValidator.UsesNearestFoeTarget(recallOnly));
        Assert.False(WorkingValidator.UsesNearestFoeTarget(orphanNearest));
    }

    [Fact]
    public void Validate_RejectsMoreThanSevenOrRepeatedClauses()
    {
        var working = new Working("working.too_large", "working.too_large");

        for (int id = 1; id <= WorkingValidator.MaxNodeCount + 1; id++)
        {
            working.AddNode(new WorkingNode(id, "clause.damage_them"));
        }

        var issues = WorkingValidator.Validate(working);

        Assert.Contains(issues, issue => issue.Contains("at most 7 clauses"));
        Assert.Contains(issues, issue => issue.Contains("each clause can appear only once"));
    }

    [Fact]
    public void Execute_InvalidWorkingFailsBeforeEarlierClausesCanMutateWorld()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(3, 1), health: 3);
        var working = new Working("working.late_error", "working.late_error");
        var aim = new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 };
        var damage = new WorkingNode(2, "clause.damage_them") { NextNodeId = 3 };
        var invalid = new WorkingNode(3, "clause.not_registered");
        working.AddNode(aim);
        working.AddNode(damage);
        working.AddNode(invalid);

        WorkingResult result = new WorkingMachine().Execute(
            working,
            new EncounterSpellWorld(encounter),
            encounter.Player.Id,
            enemy.Position);

        Assert.False(result.Succeeded);
        Assert.False(result.ChangedWorld);
        Assert.Equal("Invalid working.", result.FailureReason);
        Assert.Equal(3, enemy.Health);
        Assert.Contains(result.Trace.Events, traceEvent => traceEvent.Text.Contains("working is invalid"));
        Assert.Contains(result.Trace.Events, traceEvent => traceEvent.Text.Contains("clause.not_registered"));
    }

    [Fact]
    public void TryCast_InvalidWorkingDoesNotSpendThePlayerTurn()
    {
        var encounter = new TacticalEncounter(5, 5, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(3, 1));
        var working = new Working("working.invalid", "working.invalid");
        working.AddNode(new WorkingNode(1, "clause.not_registered"));

        WorkingResult result = encounter.TryCastWorking(working, new GridPos(3, 1));

        Assert.False(result.Succeeded);
        Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);
        Assert.Equal(1, encounter.Turns.Round);
    }

    private static Working SingleNodeWorking()
    {
        var working = new Working("working.valid", "working.valid");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target"));
        return working;
    }
}
