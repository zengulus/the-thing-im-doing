using System;

namespace TheThingImDoing.Core;

public enum TurnPhase
{
    PlayerTurn,
    EnemyTurn
}

public sealed class TurnSystem
{
    public TurnSystem()
    {
    }

    public TurnSystem(TurnPhase phase, int round)
    {
        Phase = phase;
        Round = round;
    }

    public TurnPhase Phase { get; private set; } = TurnPhase.PlayerTurn;
    public int Round { get; private set; } = 1;

    public event Action<TurnPhase>? PhaseChanged;

    public void EndPlayerTurn()
    {
        if (Phase != TurnPhase.PlayerTurn)
        {
            return;
        }

        Phase = TurnPhase.EnemyTurn;
        PhaseChanged?.Invoke(Phase);
    }

    public void EndEnemyTurn()
    {
        if (Phase != TurnPhase.EnemyTurn)
        {
            return;
        }

        Round++;
        Phase = TurnPhase.PlayerTurn;
        PhaseChanged?.Invoke(Phase);
    }

    public TurnSystem Clone()
    {
        return new TurnSystem(Phase, Round);
    }
}
