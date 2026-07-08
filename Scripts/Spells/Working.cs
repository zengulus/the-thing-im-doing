using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Spells;

public sealed class Working
{
    private readonly List<WorkingNode> _nodes = new();

    public Working(string displayName)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; set; }
    public int MaxSteps { get; set; } = 24;
    public int? EntryNodeId { get; set; }
    public IReadOnlyList<WorkingNode> Nodes => _nodes;

    public string EstimatedCounterSummary
    {
        get
        {
            var costs = new Dictionary<string, int>();
            var gains = new Dictionary<string, int>();

            foreach (WorkingNode node in _nodes)
            {
                if (!ClauseDefinitionCatalog.TryGet(node.ClauseId, out ClauseDefinition? definition))
                {
                    continue;
                }

                AddAll(costs, definition.CounterCosts);
                AddAll(gains, definition.CounterGains);
            }

            return ClauseDefinition.FormatCounters(costs, gains);
        }
    }

    public void AddNode(WorkingNode node)
    {
        if (_nodes.Any(existing => existing.Id == node.Id))
        {
            return;
        }

        _nodes.Add(node);
        EntryNodeId ??= node.Id;
    }

    public void RemoveNode(int nodeId)
    {
        _nodes.RemoveAll(node => node.Id == nodeId);

        foreach (WorkingNode node in _nodes)
        {
            node.ClearConnectionsTo(nodeId);
        }

        if (EntryNodeId == nodeId)
        {
            EntryNodeId = _nodes.FirstOrDefault()?.Id;
        }
    }

    public WorkingNode? GetNode(int nodeId)
    {
        return _nodes.FirstOrDefault(node => node.Id == nodeId);
    }

    public int GetNextAvailableNodeId()
    {
        return _nodes.Count == 0 ? 1 : _nodes.Max(node => node.Id) + 1;
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

    public Working Clone()
    {
        var clone = new Working(DisplayName)
        {
            MaxSteps = MaxSteps,
            EntryNodeId = EntryNodeId
        };

        foreach (WorkingNode node in _nodes)
        {
            clone.AddNode(node.Clone());
        }

        return clone;
    }
}
