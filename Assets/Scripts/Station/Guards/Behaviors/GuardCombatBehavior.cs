using UnityEngine;

namespace Pulsar.Stations.Guards
{
/// <summary>
/// How a guard fights once it's chasing the player. The guard shell carries one
/// of each (Bombard, Fighter, Orbit); the generator keeps one at random.
/// Each is a small state machine that EnemyGuardAI ticks every physics step.
/// </summary>
public abstract class GuardCombatBehavior : MonoBehaviour
{
    /// <summary>The current state's name, shown on EnemyGuardAI for debugging.</summary>
    public abstract string CurrentStateName { get; }

    /// <summary>Start the cycle over from its first state (Move).</summary>
    public abstract void ResetCycle();

    public abstract void Tick(EnemyGuardAI guard, Vector2 target, float deltaTime);
}
}
