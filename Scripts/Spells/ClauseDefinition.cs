using System.Collections.Generic;
using TheThingImDoing.Content;

namespace TheThingImDoing.Spells;

public sealed record ClauseDefinition(
    string Id,
    string DisplayNameKey,
    string PlayerTextKey,
    ClauseFamily Family,
    int BaseFocusCost,
    string TooltipKey,
    string BehaviorId,
    bool IsCondition = false,
    IReadOnlyList<string>? Tags = null)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string PlayerText => GameStrings.Get(PlayerTextKey);
    public string Tooltip => GameStrings.Get(TooltipKey);
}
