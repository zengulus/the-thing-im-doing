using System;

namespace TheThingImDoing.Core;

public sealed class GameRunSession
{
    public GameRunSession(string runId)
        : this(RunDefinitionCatalog.Get(runId), seed: 0)
    {
    }

    public GameRunSession(string runId, int seed)
        : this(RunDefinitionCatalog.Get(runId), seed)
    {
    }

    public GameRunSession(RunDefinition definition)
        : this(definition, seed: 0)
    {
    }

    public GameRunSession(RunDefinition definition, int seed)
    {
        Definition = definition;
        Seed = seed;

        if (definition.EncounterIds.Count == 0)
        {
            throw new ArgumentException("A run must contain at least one encounter.", nameof(definition));
        }
    }

    public RunDefinition Definition { get; }
    public int Seed { get; }
    public int CurrentEncounterIndex { get; private set; }
    public int Victories { get; private set; }
    public bool IsComplete => CurrentEncounterIndex >= Definition.EncounterIds.Count;
    public string? CurrentEncounterId => IsComplete ? null : Definition.EncounterIds[CurrentEncounterIndex];
    public EncounterDefinition? CurrentEncounter => CurrentEncounterId == null
        ? null
        : EncounterDefinitionCatalog.Get(CurrentEncounterId);
    public int? CurrentEncounterSeed => CurrentEncounter?.Generation == null
        ? null
        : MixSeed(Seed, CurrentEncounter.Generation.Seed, CurrentEncounterIndex);

    public bool TryAdvance(GameResult encounterResult)
    {
        if (IsComplete || encounterResult != GameResult.PlayerWon)
        {
            return false;
        }

        Victories++;
        CurrentEncounterIndex++;
        return true;
    }

    private static int MixSeed(int runSeed, int templateSeed, int encounterIndex)
    {
        unchecked
        {
            uint mixed = (uint)runSeed + 0x9E3779B9u;
            mixed ^= (uint)templateSeed + 0x85EBCA6Bu + (mixed << 6) + (mixed >> 2);
            mixed ^= (uint)encounterIndex * 0xC2B2AE35u;
            mixed ^= mixed >> 16;
            mixed *= 0x7FEB352Du;
            mixed ^= mixed >> 15;
            return (int)(mixed & 0x7FFFFFFF);
        }
    }
}
