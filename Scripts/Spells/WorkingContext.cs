using System.Collections.Generic;
using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public sealed class WorkingContext
{
    private readonly Dictionary<string, int> _counterCosts = new();
    private readonly Dictionary<string, int> _counterGains = new();

    public required int CasterActorId { get; init; }
    public required GridPos SelectedTarget { get; init; }
    public required int StepLimit { get; init; }

    public GridPos? FocusTile { get; set; }
    public int? FocusActorId { get; set; }
    public GridPos? RememberedTile { get; set; }
    public int? RememberedActorId { get; set; }
    public IReadOnlyDictionary<string, int> CounterCosts => _counterCosts;
    public IReadOnlyDictionary<string, int> CounterGains => _counterGains;

    public void RecordCounterChanges(ClauseDefinition definition)
    {
        AddAll(_counterCosts, definition.CounterCosts);
        AddAll(_counterGains, definition.CounterGains);
    }

    private static void AddAll(Dictionary<string, int> target, IReadOnlyDictionary<string, int> source)
    {
        foreach ((string counterId, int amount) in source)
        {
            if (amount == 0)
            {
                continue;
            }

            target[counterId] = target.GetValueOrDefault(counterId) + amount;
        }
    }
}
