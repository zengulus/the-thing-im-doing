namespace TheThingImDoing.Spells;

public sealed class WorkingResult
{
    private WorkingResult(bool succeeded, OmenTrace trace, string? failureReason, bool changedWorld, int focusSpent)
    {
        Succeeded = succeeded;
        Trace = trace;
        FailureReason = failureReason;
        ChangedWorld = changedWorld;
        FocusSpent = focusSpent;
    }

    public bool Succeeded { get; }
    public OmenTrace Trace { get; }
    public string? FailureReason { get; }
    public bool ChangedWorld { get; }
    public int FocusSpent { get; }

    public static WorkingResult Success(OmenTrace trace, bool changedWorld, int focusSpent)
    {
        return new WorkingResult(true, trace, null, changedWorld, focusSpent);
    }

    public static WorkingResult Failed(
        OmenTrace trace,
        string failureReason,
        bool changedWorld = false,
        int focusSpent = 0)
    {
        return new WorkingResult(false, trace, failureReason, changedWorld, focusSpent);
    }
}
