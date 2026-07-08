using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Spells;

public sealed class OmenTrace
{
    private readonly List<OmenTraceEvent> _events = new();

    public IReadOnlyList<OmenTraceEvent> Events => _events;

    public void Add(string text)
    {
        _events.Add(new OmenTraceEvent(_events.Count + 1, text));
    }

    public string ToDisplayText()
    {
        return string.Join("\n", _events.Select(traceEvent => $"{traceEvent.Step}. {traceEvent.Text}"));
    }
}

