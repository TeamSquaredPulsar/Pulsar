using UnityEngine;

namespace Pulsar.Stations.Guards
{
/// <summary>
/// Fighter (Connor's design): strafing runs in a figure 8 centred on the player.
/// AttackRun -> Pass -> Loop -> AttackRun.
/// AttackRun: full speed at the player, firing whenever it's lined up and in range.
/// Pass: flies straight on past the player without turning back (leaves combat).
/// Loop: swings around to face the player again. Each loop turns the opposite way
/// to the last one (left, then right), which is what draws the 8 instead of a circle.
/// </summary>
public class FighterBehavior : GuardCombatBehavior
{
    [Tooltip(
        "Starts the pass (flies on through) once it's this close to the player.")]
    [Min(0f)]
    [SerializeField]
    private float passDistance = 2.5f;

    [Tooltip(
        "Breaks off the pass and starts looping back at this many times the shooting range.")]
    [Min(1f)]
    [SerializeField]
    private float breakAwayFactor = 1.25f;

    [Tooltip(
        "How fast the loop turns its flight path, in degrees per second. Higher = tighter loop.")]
    [Min(1f)]
    [SerializeField]
    private float loopDegreesPerSecond = 90f;

    [Tooltip(
        "The loop ends once its flight path points within this many degrees of the player.")]
    [Range(1f, 90f)]
    [SerializeField]
    private float lineUpDegrees = 15f;

    [Tooltip("Safety net: a loop never lasts longer than this, in seconds.")]
    [Min(0.5f)]
    [SerializeField]
    private float maxLoopSeconds = 6f;

    // The direction it's flying during Pass and Loop.
    private Vector2 _heading;
    private float _lastDistance = float.MaxValue;

    // +1 turns left, -1 turns right. Flips at the start of every loop.
    private float _loopSide = 1f;
    private float _loopTimer;

    private State _state = State.AttackRun;

    public override string CurrentStateName => $"Fighter: {_state}";

    public override void ResetCycle()
    {
        _state = State.AttackRun;
        _lastDistance = float.MaxValue;
    }

    public override void Tick(
        EnemyGuardAI guard,
        Vector2 target,
        float deltaTime)
    {
        Vector2 toTarget = target - guard.Position;
        float distance = toTarget.magnitude;
        Vector2 direction = EnemyGuardAI.Direction(toTarget);

        switch (_state)
        {
            case State.AttackRun:
                guard.Face(toTarget);
                guard.Steer(direction);
                if (guard.InRange(distance))
                {
                    guard.TryFire(toTarget);
                }

                // Close enough, or it's in range but already moving away
                // (it went past to one side): fly on through.
                bool missed = guard.InRange(distance) &&
                              distance > _lastDistance;
                if (distance <= passDistance || missed)
                {
                    _heading = direction.sqrMagnitude > 0f
                        ? direction
                        : guard.Forward;
                    _state = State.Pass;
                }

                break;

            case State.Pass:
                guard.Face(_heading);
                guard.Steer(_heading);
                if (distance >= guard.Info.shootingRange * breakAwayFactor)
                {
                    _loopSide = -_loopSide;
                    _loopTimer = maxLoopSeconds;
                    _state = State.Loop;
                }

                break;

            case State.Loop:
                _heading = Rotate(_heading,
                    loopDegreesPerSecond * _loopSide * deltaTime);
                guard.Face(_heading);
                guard.Steer(_heading);
                _loopTimer -= deltaTime;
                if (Vector2.Angle(_heading, toTarget) <= lineUpDegrees ||
                    _loopTimer <= 0f)
                {
                    _state = State.AttackRun;
                    _lastDistance = float.MaxValue;
                    return;
                }

                break;
        }

        _lastDistance = distance;
    }

    private static Vector2 Rotate(Vector2 v, float degrees) =>
        Quaternion.Euler(0f, 0f, degrees) * v;

    private enum State
    {
        AttackRun, Pass, Loop,
    }
}
}
