namespace TheThingImDoing.Spells;

public sealed record OmenTraceEvent(int Step, string Text, int? WorkingNodeId = null);
