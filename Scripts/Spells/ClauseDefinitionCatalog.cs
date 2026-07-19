using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TheThingImDoing.Behaviors;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public static class ClauseDefinitionCatalog
{
    private static readonly Lazy<ContentRegistryResult<ClauseDefinition>> Registry = new(LoadRegistry);

    public static IEnumerable<ClauseDefinition> All => Registry.Value.Definitions.Values;

    public static ClauseDefinition Get(string id)
    {
        return Registry.Value.Definitions[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out ClauseDefinition? definition)
    {
        return Registry.Value.Definitions.TryGetValue(id, out definition);
    }

    public static bool IsDisabled(string id)
    {
        return Registry.Value.DisabledIds.Contains(id);
    }

    private static ContentRegistryResult<ClauseDefinition> LoadRegistry()
    {
        ContentRegistryResult<ClauseDefinition> result = ContentRegistry.Build(
            "clause",
            ContentJsonLoader.LoadItemsWithSources<ClauseContentFile, ClauseContentDefinition>(
                "clauses.json",
                file => file.Clauses),
            Resolve);

        return new ContentRegistryResult<ClauseDefinition>(
            result.Definitions
                .OrderBy(pair => pair.Value.Family)
                .ThenBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            result.DisabledIds);
    }

    private static ContentValidationResult<ClauseDefinition> Resolve(ClauseContentDefinition content)
    {
        var issues = new List<string>();
        Dictionary<string, int> counterCosts = content.CounterCosts ?? [];
        Dictionary<string, int> counterGains = content.CounterGains ?? [];
        string[] tags = (content.Tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag!)
            .ToArray();

        RequireStringKey(content.DisplayNameKey, nameof(content.DisplayNameKey), issues);
        RequireStringKey(content.PlayerTextKey, nameof(content.PlayerTextKey), issues);

        if (!string.IsNullOrWhiteSpace(content.TooltipKey))
        {
            RequireStringKey(content.TooltipKey, nameof(content.TooltipKey), issues);
        }

        if (!Enum.TryParse(content.Family, ignoreCase: true, out ClauseFamily family)
            || !Enum.IsDefined(family)
            || int.TryParse(content.Family, out _))
        {
            issues.Add($"family '{content.Family}' is invalid.");
        }

        if (!TryParseRole(content.Role, out ClauseRole role))
        {
            issues.Add(
                $"role '{content.Role}' is invalid; expected exactly one of Generator, Operator, or Consumer.");
        }

        ValidateBehaviorReference(content.BehaviorId, issues);
        ValidateConsumerSetupContract(content.BehaviorId, role, issues);
        ValidateDirectDamageContract(content.BehaviorId, role, tags, issues);
        ValidateCounters(counterCosts, nameof(content.CounterCosts), issues);
        ValidateCounters(counterGains, nameof(content.CounterGains), issues);

        if (ContentRegistry.HasAnyIssue(issues, out IReadOnlyList<string> issueList))
        {
            return new ContentValidationResult<ClauseDefinition>(null, issueList);
        }

        return ContentRegistry.Valid(new ClauseDefinition(
            content.Id?.Trim() ?? "",
            content.DisplayNameKey ?? "",
            content.PlayerTextKey ?? "",
            family,
            role,
            counterCosts,
            counterGains,
            content.TooltipKey ?? "",
            content.BehaviorId ?? "",
            content.IsCondition,
            tags));
    }

    private static bool TryParseRole(string? value, out ClauseRole role)
    {
        string candidate = value?.Trim() ?? "";

        return Enum.TryParse(candidate, ignoreCase: true, out role)
            && Enum.IsDefined(role)
            && string.Equals(Enum.GetName(role), candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static void RequireStringKey(string? key, string field, List<string> issues)
    {
        if (!ContentRegistry.HasString(key ?? ""))
        {
            issues.Add($"{field} references missing string key '{key}'.");
        }
    }

    private static void ValidateBehaviorReference(string? behaviorId, List<string> issues)
    {
        string id = behaviorId ?? "";

        if (BehaviorDefinitionCatalog.IsDisabled(id))
        {
            issues.Add($"behaviorId references disabled behavior '{behaviorId}'.");
        }
        else if (!BehaviorDefinitionCatalog.TryGet(id, out _))
        {
            issues.Add($"behaviorId references missing behavior '{behaviorId}'.");
        }
    }

    private static void ValidateDirectDamageContract(
        string? behaviorId,
        ClauseRole role,
        IReadOnlyCollection<string> tags,
        List<string> issues)
    {
        if (!BehaviorDefinitionCatalog.TryGet(behaviorId ?? "", out BehaviorDefinition? behavior))
        {
            return;
        }

        issues.AddRange(ValidateDirectDamageContract(behavior, role, tags));
    }

    public static IReadOnlyList<string> ValidateDirectDamageContract(
        BehaviorDefinition behavior,
        ClauseRole role,
        IReadOnlyCollection<string> tags)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(tags);

        var issues = new List<string>();

        bool hasDirectDamage = behavior.Steps.Any(step => step.Op == "damage.apply");
        bool hasNestedDirectDamage = ContainsNestedDirectDamage(
            behavior,
            new HashSet<string>(StringComparer.Ordinal) { behavior.Id });

        if (!hasDirectDamage && !hasNestedDirectDamage)
        {
            return issues;
        }

        if (role != ClauseRole.Consumer)
        {
            issues.Add("a clause behavior containing direct damage must have role Consumer.");
        }

        if (!tags.Contains("damage", StringComparer.Ordinal))
        {
            issues.Add("a clause behavior containing direct damage must declare the 'damage' tag.");
        }

        if (hasNestedDirectDamage)
        {
            issues.Add(
                "direct damage may not be hidden inside a composite primitive; expose damage.apply in the " +
                "clause behavior so its setup path can be validated.");
        }

        if (hasDirectDamage && !EveryDamagePathConsumesSetup(behavior))
        {
            issues.Add(
                "every path to direct damage must first confirm and consume matching effect or counter setup.");
        }

        return issues;
    }

    private static bool ContainsNestedDirectDamage(
        BehaviorDefinition behavior,
        HashSet<string> visitedBehaviorIds)
    {
        foreach (BehaviorStepDefinition step in behavior.Steps)
        {
            if (!BehaviorPrimitiveCatalog.TryGet(
                    step.Op,
                    out BehaviorPrimitiveDefinition? primitive)
                || string.IsNullOrWhiteSpace(primitive.BehaviorId)
                || !visitedBehaviorIds.Add(primitive.BehaviorId)
                || !BehaviorDefinitionCatalog.TryGet(
                    primitive.BehaviorId,
                    out BehaviorDefinition? nested))
            {
                continue;
            }

            if (nested.Steps.Any(nestedStep => nestedStep.Op == "damage.apply")
                || ContainsNestedDirectDamage(nested, visitedBehaviorIds))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateConsumerSetupContract(
        string? behaviorId,
        ClauseRole role,
        List<string> issues)
    {
        if (role != ClauseRole.Consumer
            || !BehaviorDefinitionCatalog.TryGet(behaviorId ?? "", out BehaviorDefinition? behavior))
        {
            return;
        }

        bool hasSetupGate = behavior.Steps.Any(step =>
            step.Op is "effect.has" or "counter.at_least" or "counter.consume"
            || step.Op.StartsWith("branch.", StringComparison.Ordinal));

        if (!hasSetupGate)
        {
            issues.Add(
                "a Consumer behavior must gate its payoff on existing effect, counter, positional, or world setup.");
        }
    }

    private static bool EveryDamagePathConsumesSetup(BehaviorDefinition behavior)
    {
        Dictionary<int, BehaviorStepDefinition> steps = behavior.Steps.ToDictionary(step => step.Id);
        var pending = new Stack<DamagePathState>();
        var visited = new HashSet<DamagePathState>();
        pending.Push(new DamagePathState(behavior.Steps[0].Id, "", SetupConsumed: false));

        while (pending.TryPop(out DamagePathState state))
        {
            if (!visited.Add(state))
            {
                continue;
            }

            BehaviorStepDefinition step = steps[state.StepId];

            if (step.Op == "damage.apply" && (state.SetupKey.Length == 0 || !state.SetupConsumed))
            {
                return false;
            }

            if (step.Op == "flow.stop")
            {
                continue;
            }

            if (step.Op == "effect.has")
            {
                string setupKey = EffectSetupKey(step.Effect, step.Source);
                PushBranch(
                    step,
                    step.True ?? step.Next,
                    setupKey.Length == 0 ? state : state with { SetupKey = setupKey });
                PushBranch(step, step.False ?? step.Next, state);
                continue;
            }

            if (step.Op == "counter.at_least")
            {
                string setupKey = CounterSetupKey(step);
                PushBranch(
                    step,
                    step.True ?? step.Next,
                    (step.Amount ?? 1) <= 0 || setupKey.Length == 0
                        ? state
                        : state with { SetupKey = setupKey });
                PushBranch(step, step.False ?? step.Next, state);
                continue;
            }

            if (step.Op == "counter.consume")
            {
                string setupKey = CounterSetupKey(step);
                bool consumesSetup = (step.Amount ?? 1) > 0 && setupKey.Length > 0;
                PushBranch(
                    step,
                    step.True ?? step.Next,
                    consumesSetup
                        ? state with
                        {
                            SetupKey = setupKey,
                            SetupConsumed = true
                        }
                        : state);
                PushBranch(step, step.False ?? step.Next, state);
                continue;
            }

            DamagePathState nextState = state;

            if (step.Op == "effect.detach"
                && step.Target == "focus.effect"
                && !string.IsNullOrWhiteSpace(step.Owner)
                && state.SetupKey == EffectSetupKey(step.Effect, step.Owner))
            {
                nextState = state with { SetupConsumed = true };
            }

            if (IsBranching(step.Op))
            {
                PushBranch(step, step.True ?? step.Next, nextState);
                PushBranch(step, step.False ?? step.Next, nextState);
                continue;
            }

            Push(step.Next ?? GetImplicitNext(behavior, step.Id), nextState);
        }

        return true;

        void PushBranch(
            BehaviorStepDefinition branch,
            int? stepId,
            DamagePathState nextState)
        {
            if (branch.True.HasValue || branch.False.HasValue)
            {
                Push(stepId, nextState);
            }
        }

        void Push(int? stepId, DamagePathState nextState)
        {
            if (stepId.HasValue)
            {
                pending.Push(nextState with { StepId = stepId.Value });
            }
        }
    }

    private static bool IsBranching(string op)
    {
        return op.StartsWith("branch.", StringComparison.Ordinal);
    }

    private static string EffectSetupKey(string effect, string owner)
    {
        if (string.IsNullOrWhiteSpace(effect) || string.IsNullOrWhiteSpace(owner))
        {
            return "";
        }

        return $"effect:{effect}:{owner}";
    }

    private static string CounterSetupKey(BehaviorStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.Target) || string.IsNullOrWhiteSpace(step.Counter))
        {
            return "";
        }

        return $"counter:{step.Target}:{step.Effect}:{step.Counter}";
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

    private static void ValidateCounters(Dictionary<string, int> counters, string field, List<string> issues)
    {
        foreach ((string counterId, int amount) in counters)
        {
            if (string.IsNullOrWhiteSpace(counterId))
            {
                issues.Add($"{field} contains a blank counter id.");
            }

            if (amount < 0)
            {
                issues.Add($"{field} contains negative amount {amount} for '{counterId}'.");
            }
        }
    }

    private sealed class ClauseContentFile : IContentFile
    {
        public int SchemaVersion { get; set; }
        public List<ClauseContentDefinition> Clauses { get; set; } = [];
    }

    private readonly record struct DamagePathState(
        int StepId,
        string SetupKey,
        bool SetupConsumed);

    private sealed class ClauseContentDefinition : IContentDefinition
    {
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string? DisplayNameKey { get; set; } = "";
        public string? PlayerTextKey { get; set; } = "";
        public string? TooltipKey { get; set; } = "";
        public string? Family { get; set; } = "";
        public string? Role { get; set; } = "";
        public Dictionary<string, int>? CounterCosts { get; set; } = [];
        public Dictionary<string, int>? CounterGains { get; set; } = [];
        public string? BehaviorId { get; set; } = "";
        public bool IsCondition { get; set; }
        public List<string?>? Tags { get; set; } = [];
    }
}
