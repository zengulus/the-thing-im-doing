using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Spells;

public sealed class WorkingResult
{
    private WorkingResult(
        bool succeeded,
        OmenTrace trace,
        string? failureReason,
        bool changedWorld,
        IReadOnlyDictionary<string, int> counterCosts,
        IReadOnlyDictionary<string, int> counterGains)
    {
        Succeeded = succeeded;
        Trace = trace;
        FailureReason = failureReason;
        ChangedWorld = changedWorld;
        CounterCosts = CopyNonZero(counterCosts);
        CounterGains = CopyNonZero(counterGains);
    }

    public bool Succeeded { get; }
    public OmenTrace Trace { get; }
    public string? FailureReason { get; }
    public bool ChangedWorld { get; }
    public IReadOnlyDictionary<string, int> CounterCosts { get; }
    public IReadOnlyDictionary<string, int> CounterGains { get; }
    public string CounterSummary => ClauseDefinition.FormatCounters(CounterCosts, CounterGains);

    public static WorkingResult Success(
        OmenTrace trace,
        bool changedWorld,
        IReadOnlyDictionary<string, int> counterCosts,
        IReadOnlyDictionary<string, int> counterGains)
    {
        return new WorkingResult(true, trace, null, changedWorld, counterCosts, counterGains);
    }

    public static WorkingResult Failed(
        OmenTrace trace,
        string failureReason,
        bool changedWorld = false,
        IReadOnlyDictionary<string, int>? counterCosts = null,
        IReadOnlyDictionary<string, int>? counterGains = null)
    {
        return new WorkingResult(
            false,
            trace,
            failureReason,
            changedWorld,
            counterCosts ?? new Dictionary<string, int>(),
            counterGains ?? new Dictionary<string, int>());
    }

    private static IReadOnlyDictionary<string, int> CopyNonZero(IReadOnlyDictionary<string, int> counters)
    {
        return counters
            .Where(pair => pair.Value != 0)
            .OrderBy(pair => pair.Key)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
