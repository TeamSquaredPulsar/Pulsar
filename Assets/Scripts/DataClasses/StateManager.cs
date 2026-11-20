using System;

namespace Pulsar.DataClasses
{
public static class StateManager
{
    public static event Action<GameState> GameStateChanged;

    public static GameState CurrentState
    {
        get;
        private set;
    } = GameState.Loading;

    public static void SetCurrentState(GameState newState)
    {
        CurrentState = newState;
        GameStateChanged?.Invoke(CurrentState);
    }
}
}
