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

    public int EstimatedFocusCost
    {
        get
        {
            return _nodes.Sum(node => ClauseDefinitionCatalog.TryGet(node.ClauseId, out ClauseDefinition? definition)
                ? definition.BaseFocusCost
                : 0);
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
