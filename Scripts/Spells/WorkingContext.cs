using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public sealed class WorkingContext
{
    public required int CasterActorId { get; init; }
    public required GridPos SelectedTarget { get; init; }
    public required int StepLimit { get; init; }

    public GridPos? FocusTile { get; set; }
    public int? FocusActorId { get; set; }
    public GridPos? RememberedTile { get; set; }
    public int? RememberedActorId { get; set; }
    public int FocusSpent { get; set; }
}

