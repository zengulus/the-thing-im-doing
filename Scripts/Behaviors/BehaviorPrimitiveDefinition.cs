using System.Collections.Generic;
using TheThingImDoing.Content;

namespace TheThingImDoing.Behaviors;

public sealed record BehaviorPrimitiveDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    string? BehaviorId = null,
    IReadOnlyList<string>? Scopes = null,
    IReadOnlyList<string>? Tags = null)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Description => GameStrings.Get(DescriptionKey);
}
