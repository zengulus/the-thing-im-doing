using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TheThingImDoing.Spells;

public sealed class WorkingData
{
    public int SchemaVersion { get; set; } = Working.CurrentSchemaVersion;
    public string Id { get; set; } = "";
    public string DisplayNameKey { get; set; } = "";
    public int MaxSteps { get; set; } = 24;
    public int? EntryNodeId { get; set; }
    public List<WorkingNodeData> Nodes { get; set; } = [];
    public Dictionary<int, WorkingNodeLayoutData> Layout { get; set; } = [];
}

public sealed class WorkingNodeData
{
    public int Id { get; set; }
    public string ClauseId { get; set; } = "";
    public int? NextNodeId { get; set; }
    public int? TrueNodeId { get; set; }
    public int? FalseNodeId { get; set; }
}

public sealed class WorkingNodeLayoutData
{
    public float X { get; set; }
    public float Y { get; set; }
}

public static class WorkingJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static WorkingData ToData(this Working working)
    {
        return new WorkingData
        {
            SchemaVersion = working.SchemaVersion,
            Id = working.Id,
            DisplayNameKey = working.DisplayNameKey,
            MaxSteps = working.MaxSteps,
            EntryNodeId = working.EntryNodeId,
            Nodes = working.Nodes
                .Select(node => new WorkingNodeData
                {
                    Id = node.Id,
                    ClauseId = node.ClauseId,
                    NextNodeId = node.NextNodeId,
                    TrueNodeId = node.TrueNodeId,
                    FalseNodeId = node.FalseNodeId
                })
                .ToList(),
            Layout = working.Layout.ToDictionary(
                pair => pair.Key,
                pair => new WorkingNodeLayoutData { X = pair.Value.X, Y = pair.Value.Y })
        };
    }

    public static Working FromData(WorkingData data)
    {
        if (data.SchemaVersion != Working.CurrentSchemaVersion)
        {
            throw new JsonException($"Unsupported Working schemaVersion {data.SchemaVersion}.");
        }

        var working = new Working(data.Id, data.DisplayNameKey)
        {
            SchemaVersion = data.SchemaVersion,
            MaxSteps = data.MaxSteps
        };

        foreach (WorkingNodeData node in data.Nodes)
        {
            working.AddNode(new WorkingNode(node.Id, node.ClauseId)
            {
                NextNodeId = node.NextNodeId,
                TrueNodeId = node.TrueNodeId,
                FalseNodeId = node.FalseNodeId
            });
        }

        foreach ((int nodeId, WorkingNodeLayoutData layout) in data.Layout)
        {
            working.SetNodeLayout(nodeId, new WorkingNodeLayout(layout.X, layout.Y));
        }

        working.EntryNodeId = data.EntryNodeId;
        return working;
    }

    public static string ToJson(this Working working)
    {
        return JsonSerializer.Serialize(working.ToData(), Options);
    }

    public static Working FromJson(string json)
    {
        WorkingData? data = JsonSerializer.Deserialize<WorkingData>(json, Options);

        if (data == null)
        {
            throw new JsonException("Working JSON was empty.");
        }

        return FromData(data);
    }
}
