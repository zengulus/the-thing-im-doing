using System.Collections.Generic;
using TheThingImDoing.Content;

namespace TheThingImDoing.Behaviors;

public sealed record BehaviorPrimitiveDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    string? BehaviorId = null,
    IReadOnlyList<string>? Scopes = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<BehaviorPrimitiveParameterDefinition>? Parameters = null)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Description => GameStrings.Get(DescriptionKey);
}

public sealed record BehaviorPrimitiveParameterDefinition(
    string Name,
    string Type,
    bool Required = false,
    string Default = "",
    IReadOnlyList<string>? AllowedValues = null);
