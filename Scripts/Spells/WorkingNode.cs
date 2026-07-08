namespace TheThingImDoing.Spells;

public sealed class WorkingNode
{
    public WorkingNode(int id, string clauseId)
    {
        Id = id;
        ClauseId = clauseId;
    }

    public int Id { get; }
    public string ClauseId { get; set; }
    public int? NextNodeId { get; set; }
    public int? TrueNodeId { get; set; }
    public int? FalseNodeId { get; set; }

    public int? GetOutput(WorkingOutputPort port)
    {
        return port switch
        {
            WorkingOutputPort.Next => NextNodeId,
            WorkingOutputPort.True => TrueNodeId,
            WorkingOutputPort.False => FalseNodeId,
            _ => null
        };
    }

    public void SetOutput(WorkingOutputPort port, int? targetNodeId)
    {
        switch (port)
        {
            case WorkingOutputPort.Next:
                NextNodeId = targetNodeId;
                break;
            case WorkingOutputPort.True:
                TrueNodeId = targetNodeId;
                break;
            case WorkingOutputPort.False:
                FalseNodeId = targetNodeId;
                break;
        }
    }

    public void ClearConnectionsTo(int targetNodeId)
    {
        if (NextNodeId == targetNodeId)
        {
            NextNodeId = null;
        }

        if (TrueNodeId == targetNodeId)
        {
            TrueNodeId = null;
        }

        if (FalseNodeId == targetNodeId)
        {
            FalseNodeId = null;
        }
    }

    public WorkingNode Clone()
    {
        return new WorkingNode(Id, ClauseId)
        {
            NextNodeId = NextNodeId,
            TrueNodeId = TrueNodeId,
            FalseNodeId = FalseNodeId
        };
    }
}
