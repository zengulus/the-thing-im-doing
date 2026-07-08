using System.Collections.Generic;
using TheThingImDoing.Core;

namespace TheThingImDoing.Spells;

public sealed class WorkingContext
{
    private readonly Dictionary<string, int> _counterCosts = new();
    private readonly Dictionary<string, int> _counterGains = new();
    private readonly Dictionary<string, WorkingReference> _references = new();

    public required int CasterActorId { get; init; }
    public required GridPos SelectedTarget { get; init; }
    public required int StepLimit { get; init; }

    public GridPos? FocusTile { get; set; }
    public int? FocusActorId { get; set; }
    public IReadOnlyDictionary<string, int> CounterCosts => _counterCosts;
    public IReadOnlyDictionary<string, int> CounterGains => _counterGains;
    public int CostAdjustment { get; private set; }

    public void StoreReference(string refId)
    {
        if (string.IsNullOrWhiteSpace(refId))
        {
            return;
        }

        _references[refId] = new WorkingReference(FocusActorId, FocusTile);
    }

    public bool TryGetReference(string refId, out WorkingReference reference)
    {
        return _references.TryGetValue(refId, out reference);
    }

    public void RecordCasterCounterCosts(ClauseDefinition definition, EncounterActor caster)
    {
        AddAll(_counterCosts, definition.CounterCosts);

        foreach ((string counterId, int amount) in definition.CounterCosts)
        {
            CostAdjustment += GetCounterCostAdjustment(caster, caster, counterId, -amount);
        }
    }

    public void RecordCasterCounterGains(ClauseDefinition definition, EncounterActor caster)
    {
        AddAll(_counterGains, definition.CounterGains);

        foreach ((string counterId, int amount) in definition.CounterGains)
        {
            CostAdjustment += GetCounterCostAdjustment(caster, caster, counterId, amount);
        }
    }

    public void RecordCounterMutation(EncounterActor caster, EncounterActor target, string counterId, int amount)
    {
        if (amount > 0)
        {
            Add(_counterGains, counterId, amount);
        }
        else if (amount < 0)
        {
            Add(_counterCosts, counterId, -amount);
        }

        CostAdjustment += GetCounterCostAdjustment(caster, target, counterId, amount);
    }

    private static void AddAll(Dictionary<string, int> target, IReadOnlyDictionary<string, int> source)
    {
        foreach ((string counterId, int amount) in source)
        {
            if (amount == 0)
            {
                continue;
            }

            target[counterId] = target.GetValueOrDefault(counterId) + amount;
        }
    }

    private static void Add(Dictionary<string, int> target, string counterId, int amount)
    {
        if (amount == 0 || string.IsNullOrWhiteSpace(counterId))
        {
            return;
        }

        target[counterId] = target.GetValueOrDefault(counterId) + amount;
    }

    private static int GetCounterCostAdjustment(
        EncounterActor caster,
        EncounterActor target,
        string counterId,
        int amount)
    {
        int valence = GetCounterValence(counterId);

        if (valence == 0 || amount == 0)
        {
            return 0;
        }

        int alignment = target.Faction == caster.Faction ? 1 : -1;
        return amount * valence * alignment;
    }

    private static int GetCounterValence(string counterId)
    {
        if (counterId.StartsWith("counter.bonus."))
        {
            return 1;
        }

        if (counterId.StartsWith("counter.malus."))
        {
            return -1;
        }

        return 0;
    }
}

public readonly record struct WorkingReference(int? ActorId, GridPos? Tile);
