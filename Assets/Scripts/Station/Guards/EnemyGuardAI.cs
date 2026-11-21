using System;
using System.Collections.Generic;
using Pulsar.Player;
using Pulsar.Ship;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Stations.Guards
{
/// <summary>
/// The brain of a station guard. Two states:
/// Roaming  - patrols a circle around its home station, never shoots.
/// Chasing  - hands control to its combat behaviour (Bombard, Fighter or Orbit).
/// The station decides which state its guards are in.
/// Movement goes through ShipManager.Move, like every other ship.
/// </summary>
[RequireComponent(typeof(ShipManager), typeof(Rigidbody2D))]
public class EnemyGuardAI : MonoBehaviour
{
    public enum GuardState
    {
        Roaming,
        Chasing,
    }

    private const float MinDistance = 0.001f;
    private const float TurnGain = 4f;

    /// <summary>Any guard was destroyed. GamePlayUI adds the points to the score.</summary>
    public static event Action<EnemyGuardAI, int> GuardDefeatedEvent;

    private static readonly HashSet<EnemyGuardAI> ActiveGuards = new();

    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private Rigidbody2D rb;

    [Header("Roaming")]
    [Tooltip("How fast the patrol point moves around the station, in degrees per second.")]
    [SerializeField]
    private float patrolDegreesPerSecond = 12f;

    [Tooltip("Share of full thrust used while patrolling.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float patrolThrottle = 0.5f;

    [Header("Station avoidance")]
    [Tooltip("Extra space kept between this guard and its station's outline.")]
    [Min(0f)]
    [SerializeField]
    private float stationGap = 1.5f;

    [Header("Debug (read only)")]
    [SerializeField]
    private GuardState state = GuardState.Roaming;

    [SerializeField]
    private string combatState;

    private GuardCombatBehavior _behavior;
    private SpaceStation _home;
    private float _patrolRadius;
    private float _patrolAngle;
    private float _fireCooldown;
    private bool _ready;

    /// <summary>This guard was destroyed. Its station counts it off the wave.</summary>
    public event Action<EnemyGuardAI> Defeated;

    public EnemyInfo Info { get; private set; }
    public GuardState State => state;
    public float Radius { get; private set; }
    public Vector2 Position => rb.position;
    public Vector2 Forward => transform.up;

    private void Awake()
    {
        if (!shipManager)
        {
            shipManager = GetComponent<ShipManager>();
        }

        if (!rb)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        shipManager.ShipDestroyedEvent += HandleShipDestroyed;
    }

    private void OnEnable() => ActiveGuards.Add(this);

    private void OnDisable() => ActiveGuards.Remove(this);

    private void OnDestroy()
    {
        shipManager.ShipDestroyedEvent -= HandleShipDestroyed;
    }

    /// <summary>
    /// Keeps one of the combat behaviours on this ship at random and removes
    /// the others. Called by the EnemyGenerator when it builds the guard.
    /// </summary>
    public void PickRandomBehavior()
    {
        GuardCombatBehavior[] options = GetComponents<GuardCombatBehavior>();
        if (options.Length == 0)
        {
            Debug.LogError($"{name} has no combat behaviour to pick from.");
            return;
        }

        _behavior = options[Random.Range(0, options.Length)];
        foreach (GuardCombatBehavior option in options)
        {
            if (option != _behavior)
            {
                Destroy(option);
            }
        }
    }

    /// <summary>Called by the station right after it spawns this guard.</summary>
    public void Init(
        SpaceStation home,
        EnemyInfo info,
        float patrolRadius,
        float startAngleDegrees)
    {
        _home = home;
        Info = info;
        _patrolRadius = patrolRadius;
        _patrolAngle = startAngleDegrees;
        _fireCooldown = info ? info.startTimeBtwShots : 1f;
        Radius = EnemyController.CalculateRadius(transform);
        _ready = info && _behavior;
        if (!info)
        {
            Debug.LogError($"{name} has no EnemyInfo (set Guard Info on the station profile).");
        }
    }

    public void SetState(GuardState newState)
    {
        if (state == newState)
        {
            return;
        }

        state = newState;
        _behavior?.ResetCycle();
    }

    private void FixedUpdate()
    {
        if (!_ready)
        {
            return;
        }

        _fireCooldown -= Time.fixedDeltaTime;

        if (state == GuardState.Roaming || !_home)
        {
            combatState = "Roaming";
            Patrol();
            return;
        }

        Vector2 playerPosition = PlayerController.PlayerPosition;
        _behavior.Tick(this, playerPosition, Time.fixedDeltaTime);
        combatState = _behavior.CurrentStateName;
    }

    // Fly after a point that slides around the station's circle. Each guard's
    // point starts at its spawn angle, so the guards stay spread around the
    // whole ring. (If a guard's path cuts across the station, AvoidStation
    // slides it around the hull.)
    private void Patrol()
    {
        _patrolAngle += patrolDegreesPerSecond * Time.fixedDeltaTime;
        float radians = _patrolAngle * Mathf.Deg2Rad;
        Vector2 point = (Vector2)_home.transform.position +
                        new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) *
                        _patrolRadius;

        Vector2 toPoint = point - Position;
        // Ease off when close, so it doesn't overshoot the point.
        float throttle = patrolThrottle * Mathf.Clamp01(toPoint.magnitude / 3f);
        Steer(Direction(toPoint) * throttle);
        Face(toPoint.sqrMagnitude > 1f ? toPoint : rb.linearVelocity);
    }

    // ------------------------------------------------ tools for the combat behaviours

    public static Vector2 Direction(Vector2 v) =>
        v.sqrMagnitude > MinDistance * MinDistance ? v.normalized : Vector2.zero;

    public bool InRange(float distance) => distance <= Info.shootingRange;

    public bool IsAimedAt(Vector2 direction) =>
        Vector2.Angle(Forward, direction) <= Info.aimTolerance;

    /// <summary>Fires every gun if the cooldown is over and it's aimed. True if it fired.</summary>
    public bool TryFire(Vector2 toTarget)
    {
        if (_fireCooldown > 0f || !IsAimedAt(toTarget))
        {
            return false;
        }

        shipManager.Fire();
        _fireCooldown = Info.startTimeBtwShots;
        return true;
    }

    /// <summary>
    /// Thrust toward a world direction (length 0..1 = throttle). Adds the push
    /// away from nearby guards so they don't stack on top of each other.
    /// </summary>
    public void Steer(Vector2 worldDirection)
    {
        Vector2 desired = worldDirection +
                          Separation() * Info.separationStrength;
        desired = AvoidStation(Vector2.ClampMagnitude(desired, 1f)) *
                  Mathf.Clamp01(Info.speed);

        Vector2 local = transform.InverseTransformDirection(desired);
        shipManager.Move(local);
    }

    /// <summary>Approach to stopping distance, back off if too close, otherwise hold.</summary>
    public void KeepDistance(Vector2 toTarget, float distance)
    {
        Vector2 direction = Direction(toTarget);
        if (distance > Info.stoppingDistance)
        {
            Steer(direction);
        }
        else if (distance < Info.retreatDistance)
        {
            Steer(-direction);
        }
        else
        {
            Steer(Vector2.zero);
        }
    }

    /// <summary>Turns the nose toward a direction (same turning as EnemyController).</summary>
    public void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < MinDistance * MinDistance)
        {
            return;
        }

        // TODO: swap for Connor's PID controller once it's merged.
        // -90 because the ship's nose is its local up.
        float targetAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float angleDiff = Mathf.DeltaAngle(rb.rotation, targetAngle);
        rb.angularVelocity = Mathf.Clamp(angleDiff * TurnGain,
            -Info.turnSpeed, Info.turnSpeed);
    }

    /// <summary>
    /// Inside the keep-out circle around the home station, the part of a move
    /// that points into the station is turned sideways, so the guard slides
    /// around the station instead of thrusting into its hull. Closer in, it
    /// also gets a push outward.
    /// </summary>
    private Vector2 AvoidStation(Vector2 desired)
    {
        if (!_home)
        {
            return desired;
        }

        Vector2 fromCentre = Position - (Vector2)_home.transform.position;
        float distance = fromCentre.magnitude;
        float keepOut = _home.Radius + Radius + stationGap;
        if (distance >= keepOut || distance < MinDistance)
        {
            return desired;
        }

        Vector2 outward = fromCentre / distance;
        float inward = -Vector2.Dot(desired, outward);
        if (inward > 0f)
        {
            // Swap the inward part for the same amount of sideways movement,
            // on whichever side the move was already leaning.
            Vector2 tangent = new(-outward.y, outward.x);
            float side = Vector2.Dot(desired, tangent) >= 0f ? 1f : -1f;
            desired += outward * inward + tangent * (inward * side);
        }

        // The deeper inside the keep-out circle, the stronger the push out.
        float depth = 1f - distance / keepOut;
        desired += outward * depth;
        return Vector2.ClampMagnitude(desired, 1f);
    }

    private Vector2 Separation()
    {
        Vector2 push = Vector2.zero;
        foreach (EnemyGuardAI other in ActiveGuards)
        {
            if (other == this)
            {
                continue;
            }

            Vector2 offset = Position - other.Position;
            float range = Radius + other.Radius + Info.separationPadding;
            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance < MinDistance * MinDistance ||
                sqrDistance >= range * range)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            push += offset * ((1f - distance / range) / distance);
        }

        return push;
    }

    private void HandleShipDestroyed(ShipManager _)
    {
        int points = Info ? Info.PointsOnKill : 0;
        Defeated?.Invoke(this);
        GuardDefeatedEvent?.Invoke(this, points);
    }
}
}
