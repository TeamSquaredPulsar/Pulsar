using UnityEngine;

namespace Pulsar.Stations.Guards
{
/// <summary>
/// Bombard: Move -> in range? -> Fire. No repositioning after firing:
/// it holds its spot and keeps shooting while the player stays in range.
/// </summary>
public class BombardBehavior : GuardCombatBehavior
{
    private enum State
    {
        Move,
        Fire,
    }

    [Tooltip("Leaves the Fire state when the player gets this many times the shooting range away.")]
    [Min(1f)]
    [SerializeField]
    private float leaveRangeFactor = 1.15f;

    private State _state = State.Move;

    public override string CurrentStateName => $"Bombard: {_state}";

    public override void ResetCycle() => _state = State.Move;

    public override void Tick(EnemyGuardAI guard, Vector2 target, float deltaTime)
    {
        Vector2 toTarget = target - guard.Position;
        float distance = toTarget.magnitude;
        guard.Face(toTarget);

        switch (_state)
        {
            case State.Move:
                guard.Steer(EnemyGuardAI.Direction(toTarget));
                if (guard.InRange(distance))
                {
                    _state = State.Fire;
                }

                break;

            case State.Fire:
                // Hold position (only backs off if the player rams it).
                if (distance < guard.Info.retreatDistance)
                {
                    guard.Steer(-EnemyGuardAI.Direction(toTarget));
                }
                else
                {
                    guard.Steer(Vector2.zero);
                }

                guard.TryFire(toTarget);
                if (distance > guard.Info.shootingRange * leaveRangeFactor)
                {
                    _state = State.Move;
                }

                break;
        }
    }
}
}
