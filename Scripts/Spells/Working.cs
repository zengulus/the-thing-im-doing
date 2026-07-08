using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public sealed class Working
{
    private readonly List<WorkingNode> _nodes = new();
    private readonly Dictionary<int, WorkingNodeLayout> _layout = new();

    public Working(string id, string displayNameKey)
    {
        Id = id;
        DisplayNameKey = displayNameKey;
    }

    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string Id { get; set; }
    public string DisplayNameKey { get; set; }
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public int MaxSteps { get; set; } = 24;
    public int? EntryNodeId { get; set; }
    public IReadOnlyList<WorkingNode> Nodes => _nodes;
    public IReadOnlyDictionary<int, WorkingNodeLayout> Layout => _layout;

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
        _layout.Remove(nodeId);

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

    public WorkingNodeLayout GetNodeLayout(int nodeId)
    {
        return _layout.TryGetValue(nodeId, out WorkingNodeLayout layout) ? layout : default;
    }

    public void SetNodeLayout(int nodeId, WorkingNodeLayout layout)
    {
        if (GetNode(nodeId) != null)
        {
            _layout[nodeId] = layout;
        }
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
        var clone = new Working(Id, DisplayNameKey)
        {
            SchemaVersion = SchemaVersion,
            MaxSteps = MaxSteps
        };

        foreach (WorkingNode node in _nodes)
        {
            clone.AddNode(node.Clone());
        }

        foreach ((int nodeId, WorkingNodeLayout layout) in _layout)
        {
            clone._layout[nodeId] = layout;
        }

        clone.EntryNodeId = EntryNodeId;
        return clone;
    }
}

public readonly record struct WorkingNodeLayout(float X, float Y);
