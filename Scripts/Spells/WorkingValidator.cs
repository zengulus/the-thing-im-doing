using System.Collections.Generic;
using System.Linq;

namespace TheThingImDoing.Spells;

public static class WorkingValidator
{
    public const int MaxNodeCount = 7;

    public static IReadOnlyList<string> Validate(Working working)
    {
        var issues = new List<string>();
        var nodeIds = working.Nodes.Select(node => node.Id).ToHashSet();

        if (working.SchemaVersion != Working.CurrentSchemaVersion)
        {
            issues.Add(
                $"Unsupported schema version {working.SchemaVersion}; expected {Working.CurrentSchemaVersion}.");
        }

        if (working.MaxSteps <= 0)
        {
            issues.Add($"Max steps must be greater than zero (was {working.MaxSteps}).");
        }

        if (working.Nodes.Count > MaxNodeCount)
        {
            issues.Add(
                $"A working can contain at most {MaxNodeCount} clauses (found {working.Nodes.Count}).");
        }

        foreach (IGrouping<string, WorkingNode> duplicate in working.Nodes
                     .GroupBy(node => node.ClauseId)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(
                $"Clause '{duplicate.Key}' appears {duplicate.Count()} times; each clause can appear only once.");
        }

        if (!working.EntryNodeId.HasValue)
        {
            issues.Add("The working has no entry node.");
        }
        else if (!nodeIds.Contains(working.EntryNodeId.Value))
        {
            issues.Add($"Entry node {working.EntryNodeId.Value} does not exist.");
        }

        foreach (WorkingNode node in working.Nodes)
        {
            ValidateReference(node.Id, "Next", node.NextNodeId, nodeIds, issues);
            ValidateReference(node.Id, "True", node.TrueNodeId, nodeIds, issues);
            ValidateReference(node.Id, "False", node.FalseNodeId, nodeIds, issues);

            if (!ClauseDefinitionCatalog.TryGet(node.ClauseId, out ClauseDefinition? definition))
            {
                issues.Add($"Node {node.Id} references unknown clause '{node.ClauseId}'.");
                continue;
            }

            if (definition.IsCondition && node.NextNodeId.HasValue)
            {
                issues.Add(
                    $"Node {node.Id} uses condition clause '{node.ClauseId}' and cannot have a Next output.");
            }

            if (!definition.IsCondition && node.TrueNodeId.HasValue)
            {
                issues.Add(
                    $"Node {node.Id} uses ordinary clause '{node.ClauseId}' and cannot have a True output.");
            }

            if (!definition.IsCondition && node.FalseNodeId.HasValue)
            {
                issues.Add(
                    $"Node {node.Id} uses ordinary clause '{node.ClauseId}' and cannot have a False output.");
            }
        }

        ValidateAllNodesReachable(working, nodeIds, issues);
        ValidateReachableCycles(working, nodeIds, issues);
        return issues;
    }

    public static bool UsesSelectedTarget(Working working)
    {
        IReadOnlySet<int> reachableNodeIds = GetReachableNodeIds(working);

        return working.Nodes.Any(node =>
            reachableNodeIds.Contains(node.Id)
            && ClauseDefinitionCatalog.TryGet(node.ClauseId, out ClauseDefinition? definition)
            && definition.Tags?.Contains("selected") == true);
    }

    public static bool UsesNearestFoeTarget(Working working)
    {
        return UsesReachableClause(working, "clause.aim_at_nearest_foe");
    }

    private static bool UsesReachableClause(Working working, string clauseId)
    {
        IReadOnlySet<int> reachableNodeIds = GetReachableNodeIds(working);
        return working.Nodes.Any(node =>
            reachableNodeIds.Contains(node.Id)
            && node.ClauseId == clauseId);
    }

    private static void ValidateReference(
        int sourceNodeId,
        string outputName,
        int? targetNodeId,
        IReadOnlySet<int> nodeIds,
        List<string> issues)
    {
        if (targetNodeId.HasValue && !nodeIds.Contains(targetNodeId.Value))
        {
            issues.Add(
                $"Node {sourceNodeId} {outputName} output references missing node {targetNodeId.Value}.");
        }
    }

    private static void ValidateReachableCycles(
        Working working,
        IReadOnlySet<int> nodeIds,
        List<string> issues)
    {
        if (!working.EntryNodeId.HasValue || !nodeIds.Contains(working.EntryNodeId.Value))
        {
            return;
        }

        var visited = new HashSet<int>();
        var active = new HashSet<int>();
        var path = new List<int>();
        var reportedCycles = new HashSet<string>();

        Visit(working.EntryNodeId.Value);

        void Visit(int nodeId)
        {
            visited.Add(nodeId);
            active.Add(nodeId);
            path.Add(nodeId);

            WorkingNode? node = working.GetNode(nodeId);

            if (node != null)
            {
                foreach (int targetNodeId in GetReferences(node))
                {
                    if (!nodeIds.Contains(targetNodeId))
                    {
                        continue;
                    }

                    if (active.Contains(targetNodeId))
                    {
                        int cycleStart = path.IndexOf(targetNodeId);
                        string cycle = string.Join(" -> ", path.Skip(cycleStart).Append(targetNodeId));

                        if (reportedCycles.Add(cycle))
                        {
                            issues.Add($"Reachable cycle detected: {cycle}.");
                        }
                    }
                    else if (!visited.Contains(targetNodeId))
                    {
                        Visit(targetNodeId);
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            active.Remove(nodeId);
        }
    }

    private static void ValidateAllNodesReachable(
        Working working,
        IReadOnlySet<int> nodeIds,
        List<string> issues)
    {
        if (!working.EntryNodeId.HasValue || !nodeIds.Contains(working.EntryNodeId.Value))
        {
            return;
        }

        IReadOnlySet<int> reachableNodeIds = GetReachableNodeIds(working);

        foreach (int nodeId in nodeIds.Where(nodeId => !reachableNodeIds.Contains(nodeId)).OrderBy(nodeId => nodeId))
        {
            issues.Add($"Node {nodeId} is disconnected from entry node {working.EntryNodeId.Value}.");
        }
    }

    private static IReadOnlySet<int> GetReachableNodeIds(Working working)
    {
        var reachable = new HashSet<int>();

        if (!working.EntryNodeId.HasValue || working.GetNode(working.EntryNodeId.Value) == null)
        {
            return reachable;
        }

        var pending = new Stack<int>();
        pending.Push(working.EntryNodeId.Value);

        while (pending.Count > 0)
        {
            int nodeId = pending.Pop();

            if (!reachable.Add(nodeId) || working.GetNode(nodeId) is not WorkingNode node)
            {
                continue;
            }

            foreach (int targetNodeId in GetReferences(node))
            {
                if (working.GetNode(targetNodeId) != null && !reachable.Contains(targetNodeId))
                {
                    pending.Push(targetNodeId);
                }
            }
        }

        return reachable;
    }

    private static IEnumerable<int> GetReferences(WorkingNode node)
    {
        if (node.NextNodeId.HasValue)
        {
            yield return node.NextNodeId.Value;
        }

        if (node.TrueNodeId.HasValue)
        {
            yield return node.TrueNodeId.Value;
        }

        if (node.FalseNodeId.HasValue)
        {
            yield return node.FalseNodeId.Value;
        }
    }
}
