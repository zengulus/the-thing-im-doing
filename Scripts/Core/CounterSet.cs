using System;
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

        if (IsCondition(counterId))
        {
            return Set(counterId, (long)Get(counterId) + amount > 0 ? 1 : 0);
        }

        long next = (long)Get(counterId) + amount;

        if (next <= 0)
        {
            _counters.Remove(counterId);
            return 0;
        }

        int clamped = (int)Math.Min(int.MaxValue, next);
        _counters[counterId] = clamped;
        return clamped;
    }

    public int Set(string counterId, int amount)
    {
        if (string.IsNullOrWhiteSpace(counterId))
        {
            return 0;
        }

        int next = IsCondition(counterId) ? Math.Clamp(amount, 0, 1) : amount;

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

    private static bool IsCondition(string counterId)
    {
        return counterId.StartsWith("condition.");
    }
}
