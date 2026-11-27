using UnityEngine;

namespace Pulsar.Hostile.Behaviors
{
/// <summary>
/// How an enemy fights the player. An enemy prefab can carry one of each
/// (Bombard, Fighter, Orbit); EnemyController keeps one at random when it
/// spawns. Each is a small state machine that EnemyController ticks every
/// physics step. A prefab with none fights the old way (chase and hold).
/// </summary>
public abstract class EnemyCombatBehavior : MonoBehaviour
{
    // The current state's name, shown on EnemyController for debugging.
    public abstract string CurrentStateName { get; }

    // Start the cycle over from its first state (Move).
    public abstract void ResetCycle();

    public abstract void Tick(EnemyController enemy, Vector2 target, float deltaTime);
}
}
