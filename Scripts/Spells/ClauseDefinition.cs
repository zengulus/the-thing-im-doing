using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public sealed record ClauseDefinition(
    string Id,
    string DisplayNameKey,
    string PlayerTextKey,
    ClauseFamily Family,
    ClauseRole Role,
    IReadOnlyDictionary<string, int> CounterCosts,
    IReadOnlyDictionary<string, int> CounterGains,
    string TooltipKey,
    string BehaviorId,
    bool IsCondition = false,
    IReadOnlyList<string>? Tags = null)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string PlayerText => GameStrings.Get(PlayerTextKey);
    public string Tooltip => GameStrings.Get(TooltipKey);
    public string CounterSummary => FormatCounters(CounterCosts, CounterGains);

    public static string FormatCounters(
        IReadOnlyDictionary<string, int> costs,
        IReadOnlyDictionary<string, int> gains)
    {
        string[] parts = costs
            .Where(pair => pair.Value != 0)
            .OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key} -{pair.Value}")
            .Concat(gains
                .Where(pair => pair.Value != 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key} +{pair.Value}"))
            .ToArray();

        return parts.Length == 0 ? "free" : string.Join(", ", parts);
    }
}
