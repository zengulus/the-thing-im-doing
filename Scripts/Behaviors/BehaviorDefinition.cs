using System.Collections.Generic;

namespace TheThingImDoing.Behaviors;

public sealed record BehaviorDefinition(string Id, IReadOnlyList<BehaviorStepDefinition> Steps);

public sealed record BehaviorStepDefinition(
    int Id,
    string Op,
    int? Next,
    int? True,
    int? False,
    int? Amount,
    int? Maximum,
    string Counter,
    string Effect,
    string Relation,
    string Ref,
    string State,
    string Target,
    string Source,
    string Mode);
