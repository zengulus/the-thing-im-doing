using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Core;

public sealed class CounterSet
{
    private readonly Dictionary<string, int> _counters = new();

    public IReadOnlyDictionary<string, int> All => _counters;

    public int Get(string counterId)
    {
        return _counters.GetValueOrDefault(counterId);
    }

    public int Add(string counterId, int amount)
    {
        if (amount == 0 || string.IsNullOrWhiteSpace(counterId))
        {
            return Get(counterId);
        }

        int next = Get(counterId) + amount;

        if (next <= 0)
        {
            _counters.Remove(counterId);
            return 0;
        }

        _counters[counterId] = next;
        return next;
    }

    public CounterSet Clone()
    {
        var clone = new CounterSet();

        foreach ((string counterId, int amount) in _counters)
        {
            clone._counters[counterId] = amount;
        }

        return clone;
    }

    public override string ToString()
    {
        return _counters.Count == 0
            ? "none"
            : string.Join(", ", _counters.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key} {pair.Value}"));
    }
}
