using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheThingImDoing.Content;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class ContentRobustnessTests
{
    [Fact]
    public void BaseContent_LoadsWithoutAnActiveGodotEngine()
    {
        TestContentDefinition[] clauses = ContentJsonLoader
            .LoadItems<TestContentFile, TestContentDefinition>("clauses.json", file => file.Clauses)
            .ToArray();

        GameStrings.Reload();

        Assert.Contains(clauses, clause => clause.Id == "clause.aim_at_target");
        Assert.Equal("Aim Target", GameStrings.Get("clauses.aim_at_target.name"));
    }

    [Fact]
    public void Build_SkipsNullItemsAndContinuesLoadingLaterDefinitions()
    {
        LoadedContentItem<TestContent>[] items =
        [
            new(null!, "null-content.json"),
            new(new TestContent("content.valid", "add", "ready"), "valid-content.json")
        ];

        ContentRegistryResult<string> result = ContentRegistry.Build<TestContent, string>(
            "test",
            items,
            content => ContentRegistry.Valid(content.Value));

        Assert.Equal("ready", result.Definitions["content.valid"]);
    }

    [Fact]
    public void Build_IsolatesResolverExceptionsAndPreservesPreviousDefinition()
    {
        LoadedContentItem<TestContent>[] items =
        [
            new(new TestContent("content.stable", "add", "base"), "base.json"),
            new(new TestContent("content.stable", "replace", "throw"), "broken-mod.json"),
            new(new TestContent("content.after", "add", "after"), "later-mod.json")
        ];

        ContentRegistryResult<string> result = ContentRegistry.Build<TestContent, string>(
            "test",
            items,
            content => content.Value == "throw"
                ? throw new InvalidOperationException("malformed nested data")
                : ContentRegistry.Valid(content.Value));

        Assert.Equal("base", result.Definitions["content.stable"]);
        Assert.Equal("after", result.Definitions["content.after"]);
    }

    [Fact]
    public void Build_SkipsRuntimeNullIdsWithoutStoppingTheRegistry()
    {
        LoadedContentItem<TestContent>[] items =
        [
            new(new TestContent(null!, "add", "broken"), "broken-mod.json"),
            new(new TestContent("content.valid", "add", "valid"), "valid-mod.json")
        ];

        ContentRegistryResult<string> result = ContentRegistry.Build<TestContent, string>(
            "test",
            items,
            content => ContentRegistry.Valid(content.Value));

        Assert.Single(result.Definitions);
        Assert.Equal("valid", result.Definitions["content.valid"]);
    }

    [Fact]
    public void MaliciousSelfAttachingEffectHook_IsBoundedAndEachEncounterCloneGetsFreshGuardState()
    {
        var guard = new EffectHookRecursionGuard();
        var trace = new OmenTrace();
        int attachmentAttempts = 0;

        AttachSelf();

        Assert.Equal(EffectHookRecursionGuard.MaximumDepth, attachmentAttempts);
        Assert.Contains(trace.Events, item =>
            item.Text == "Effect hook recursion limit reached; the nested hook was ignored.");

        var encounter = new TacticalEncounter(5, 3, new GridPos(1, 1));
        TacticalEncounter clone = encounter.Clone();
        FieldInfo guardField = typeof(TacticalEncounter).GetField(
            "_hookRecursionGuard",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.NotSame(guardField.GetValue(encounter), guardField.GetValue(clone));

        void AttachSelf()
        {
            if (!guard.TryEnter(trace, out IDisposable? scope))
            {
                return;
            }

            using (scope)
            {
                attachmentAttempts++;
                AttachSelf();
            }
        }
    }

    [Fact]
    public void BranchingEffectHookCycle_SharesOneBoundedInvocationBudgetAcrossSiblingBranches()
    {
        var guard = new EffectHookRecursionGuard();
        var trace = new OmenTrace();
        int hookInvocations = 0;

        BranchFourWays();

        Assert.Equal(EffectHookRecursionGuard.MaximumInvocationsPerChain, hookInvocations);
        Assert.Contains(trace.Events, item =>
            item.Text == "Effect hook recursion limit reached; the nested hook was ignored.");

        void BranchFourWays()
        {
            if (!guard.TryEnter(trace, out IDisposable? scope))
            {
                return;
            }

            using (scope)
            {
                hookInvocations++;

                for (int branch = 0; branch < 4; branch++)
                {
                    BranchFourWays();
                }
            }
        }
    }

    [Fact]
    public void CounterSet_AddClampsPositiveOverflowInsteadOfWrappingToZero()
    {
        var counters = new CounterSet();

        counters.Add("counter.bonus.charge", int.MaxValue);
        int value = counters.Add("counter.bonus.charge", 1);

        Assert.Equal(int.MaxValue, value);
        Assert.Equal(int.MaxValue, counters.Get("counter.bonus.charge"));
    }

    private sealed record TestContent(string Id, string Operation, string Value) : IContentDefinition;

    private sealed class TestContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<TestContentDefinition>? Clauses { get; set; }
    }

    private sealed class TestContentDefinition
    {
        public string Id { get; set; } = string.Empty;
    }
}
