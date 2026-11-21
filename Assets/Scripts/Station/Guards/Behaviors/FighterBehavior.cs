using UnityEngine;

namespace Pulsar.Stations.Guards
{
/// <summary>
/// Fighter: Move -> in range? -> Fire -> Strafe (timed) -> back to Move.
/// After every shot it slides sideways for a while, then starts the whole
/// cycle over from Move.
/// </summary>
public class FighterBehavior : GuardCombatBehavior
{
    private enum State
    {
        Move,
        Fire,
        Strafe,
    }

    [Tooltip("How long it slides sideways after firing, in seconds.")]
    [Min(0f)]
    [SerializeField]
    private float strafeSeconds = 1.5f;

    [Tooltip("Leaves the Fire state when the player gets this many times the shooting range away.")]
    [Min(1f)]
    [SerializeField]
    private float leaveRangeFactor = 1.15f;

    private State _state = State.Move;
    private float _strafeTimer;
    private float _strafeSide = 1f;

    public override string CurrentStateName => $"Fighter: {_state}";

    public override void ResetCycle() => _state = State.Move;

    public override void Tick(EnemyGuardAI guard, Vector2 target, float deltaTime)
    {
        Vector2 toTarget = target - guard.Position;
        float distance = toTarget.magnitude;
        Vector2 direction = EnemyGuardAI.Direction(toTarget);
        guard.Face(toTarget);

        switch (_state)
        {
            case State.Move:
                guard.KeepDistance(toTarget, distance);
                if (guard.InRange(distance))
                {
                    _state = State.Fire;
                }

                break;

            case State.Fire:
                guard.KeepDistance(toTarget, distance);
                if (guard.TryFire(toTarget))
                {
                    _state = State.Strafe;
                    _strafeTimer = strafeSeconds;
                    _strafeSide = Random.value < 0.5f ? -1f : 1f;
                }
                else if (distance > guard.Info.shootingRange * leaveRangeFactor)
                {
                    _state = State.Move;
                }

                break;

            case State.Strafe:
                // Sideways = at right angles to the line to the player.
                Vector2 sideways = new Vector2(-direction.y, direction.x) * _strafeSide;
                guard.Steer(sideways);
                _strafeTimer -= deltaTime;
                if (_strafeTimer <= 0f)
                {
                    _state = State.Move;
                }

                break;
        }
    }
}
}
