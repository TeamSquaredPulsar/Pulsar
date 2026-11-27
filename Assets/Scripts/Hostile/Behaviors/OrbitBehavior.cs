using UnityEngine;

namespace Pulsar.Hostile.Behaviors
{
/// <summary>
/// Orbit: Move -> InRange check -> Fire -> Reposition -> back to the InRange
/// check (not all the way back to Move like the Fighter). After each shot it
/// slides part of the way around the player at the same distance, so it stays
/// close and keeps circling.
/// </summary>
public class OrbitBehavior : EnemyCombatBehavior
{
    private enum State
    {
        Move,
        InRange,
        Fire,
        Reposition,
    }

    [Tooltip("How far around the player it slides after each shot, in degrees.")]
    [Range(0f, 180f)]
    [SerializeField]
    private float repositionDegrees = 60f;

    [Tooltip("Counts as repositioned once it's this close to the new spot.")]
    [Min(0.1f)]
    [SerializeField]
    private float arriveDistance = 1.5f;

    [Tooltip("Gives up on reaching the spot after this many seconds.")]
    [Min(0.1f)]
    [SerializeField]
    private float maxRepositionSeconds = 3f;

    private State _state = State.Move;
    private float _spotAngle;
    private float _spotDistance;
    private float _repositionTimer;

    public override string CurrentStateName => $"Orbit: {_state}";

    public override void ResetCycle() => _state = State.Move;

    public override void Tick(EnemyController enemy, Vector2 target, float deltaTime)
    {
        Vector2 toTarget = target - enemy.Position;
        float distance = toTarget.magnitude;
        enemy.Face(toTarget);

        switch (_state)
        {
            case State.Move:
                enemy.KeepDistance(toTarget, distance);
                if (enemy.InRange(distance))
                {
                    _state = State.InRange;
                }

                break;

            case State.InRange:
                // Stays in this loop: if the player slipped out of range, close in again here.
                enemy.KeepDistance(toTarget, distance);
                if (enemy.InRange(distance))
                {
                    _state = State.Fire;
                }

                break;

            case State.Fire:
                enemy.KeepDistance(toTarget, distance);
                if (enemy.TryFire(toTarget))
                {
                    StartReposition(enemy, toTarget, distance);
                }
                else if (!enemy.InRange(distance))
                {
                    _state = State.InRange;
                }

                break;

            case State.Reposition:
                // The spot is measured from the player, so it follows them if they move.
                float radians = _spotAngle * Mathf.Deg2Rad;
                Vector2 spot = target +
                               new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) *
                               _spotDistance;
                Vector2 toSpot = spot - enemy.Position;
                enemy.Steer(EnemyController.Direction(toSpot));
                _repositionTimer -= deltaTime;
                if (toSpot.magnitude <= arriveDistance || _repositionTimer <= 0f)
                {
                    _state = State.InRange;
                }

                break;
        }
    }

    private void StartReposition(EnemyController enemy, Vector2 toTarget, float distance)
    {
        // Angle of this enemy as seen from the player, then swing it around.
        Vector2 fromTarget = -toTarget;
        float currentAngle = Mathf.Atan2(fromTarget.y, fromTarget.x) * Mathf.Rad2Deg;
        float side = Random.value < 0.5f ? -1f : 1f;
        _spotAngle = currentAngle + repositionDegrees * side;
        _spotDistance = Mathf.Clamp(distance, enemy.Info.retreatDistance,
            enemy.Info.shootingRange * 0.9f);
        _repositionTimer = maxRepositionSeconds;
        _state = State.Reposition;
    }
}
}
