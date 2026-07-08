using Godot;

namespace TheThingImDoing.Actors;

public partial class FactionComponent : Node
{
    [Export] public Faction Faction { get; set; } = Faction.Neutral;

    public bool IsHostileTo(FactionComponent other)
    {
        if (Faction == Faction.Neutral || other.Faction == Faction.Neutral)
        {
            return false;
        }

        return Faction != other.Faction;
    }
}

