using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.Ship;
using Pulsar.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Stations.Guards
{
/// <summary>
/// The brain of a station guard. Guards only patrol: they circle their home
/// station, never chase the player and never shoot. Movement and turning use
/// PID controllers (like EnemyController) through ShipManager.Move and Rotate.
/// The combat tools further down (Steer, KeepDistance, TryFire...) are only
/// still here because Fighter, Bombard and Orbit call them; they move to the
/// wave enemies in the next step.
/// </summary>
[RequireComponent(typeof(ShipManager), typeof(Rigidbody2D))]
public class EnemyGuardAI : MonoBehaviour
{
    private const float MinDistance = 0.001f;

    // Any guard was destroyed. GamePlayUI adds the points to the score.
    public static event Action<EnemyGuardAI, int> GuardDefeatedEvent;

    private static readonly HashSet<EnemyGuardAI> ActiveGuards = new();

    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private Rigidbody2D rb;

    [Header("Roaming")]
    [Tooltip(
        "How fast the patrol point moves around the station, in degrees per second.")]
    [SerializeField]
    private float patrolDegreesPerSecond = 12f;

    [Tooltip("Share of full thrust used while patrolling (caps the PID's output).")]
    [Range(0f, 1f)]
    [SerializeField]
    private float patrolThrottle = 0.5f;

    [Header("Station avoidance")]
    [Tooltip("Extra space kept between this guard and its station's outline.")]
    [Min(0f)]
    [SerializeField]
    private float stationGap = 1.5f;

    private SpaceStation _home;
    private float _patrolRadius;
    private float _patrolAngle;
    private float _fireCooldown;
    private bool _ready;

    private readonly PIDController _moveXPID = new();
    private readonly PIDController _moveYPID = new();
    private readonly PIDController _turnPID = new();

    [Header("PID Tuning")]
    [Tooltip("Movement positioning/breaking strength")]
    [AllowNesting]
    [SerializeField]
    private PIDParams movePID = new(1.0f, 1.2f, 0f);

    [Tooltip("Turning positioning/breaking strength")]
    [AllowNesting]
    [SerializeField]
    private PIDParams turnPID = new(0.08f, 0.04f, .001f);

    // This guard was destroyed. Its station counts it off the wave.
    public event Action<EnemyGuardAI> Defeated;

    public EnemyInfo Info { get; private set; }
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

    // Keeps one of the combat behaviours on this ship at random and removes
    // the others. Called by the EnemyGenerator when it builds the guard.
    // Guards don't fight any more, so the kept one sits idle; this goes away
    // with the generator once enemies are prefabs.
    public void PickRandomBehavior()
    {
        GuardCombatBehavior[] options = GetComponents<GuardCombatBehavior>();
        if (options.Length == 0)
        {
            return;
        }

        GuardCombatBehavior kept = options[Random.Range(0, options.Length)];
        foreach (GuardCombatBehavior option in options)
        {
            if (option != kept)
            {
                Destroy(option);
            }
        }
    }

    // Called by the station right after it spawns this guard.
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
        _ready = info;
        if (!info)
        {
            Debug.LogError(
                $"{name} has no EnemyInfo (set Guard Info on the station profile).");
        }
    }

    private void FixedUpdate()
    {
        if (!_ready)
        {
            return;
        }

        _fireCooldown -= Time.fixedDeltaTime;

        if (!_home)
        {
            shipManager.Move(Vector2.zero);
            return;
        }

        Patrol();
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

        // Where we want to be, relative to where we are (the PID's "error"):
        // the patrol point, plus the push away from nearby guards, bent
        // around the station's hull. AvoidStation works on a direction, so
        // the distance is put back on afterwards (at least 1, so its outward
        // push still counts when the guard is right on its point).
        Vector2 offset = toPoint + Separation() * Info.separationStrength;
        float distance = offset.magnitude;
        offset = AvoidStation(Direction(offset)) * Mathf.Max(distance, 1f);

        MoveTowardOffset(offset);
        Face(toPoint.sqrMagnitude > 1f ? toPoint : rb.linearVelocity);
    }

    // PID movement, the same as EnemyController.HandleMovement: the PID
    // pushes toward the offset and brakes against the current velocity,
    // braking harder for ships whose thrusters can stop them faster.
    private void MoveTowardOffset(Vector2 worldOffset)
    {
        Vector2 localPosErr = transform.InverseTransformDirection(worldOffset);
        Vector2 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        Vector2 brakeDir = new(-Mathf.Sign(localVel.x), -Mathf.Sign(localVel.y));
        Vector2 maxBrake = shipManager.GetMaxLocalAccel(brakeDir);
        float brakeScaleX = maxBrake.x > MinDistance ? 1f / Mathf.Sqrt(maxBrake.x) : 1f;
        float brakeScaleY = maxBrake.y > MinDistance ? 1f / Mathf.Sqrt(maxBrake.y) : 1f;

        float dt = Time.fixedDeltaTime;
        float inputX = _moveXPID.CalculateVelocity(
            movePID, dt, localPosErr.x, localVel.x * brakeScaleX);
        float inputY = _moveYPID.CalculateVelocity(
            movePID, dt, localPosErr.y, localVel.y * brakeScaleY);

        shipManager.Move(new Vector2(inputX, inputY) * patrolThrottle);
    }

    // tools for the combat behaviours

    public static Vector2 Direction(Vector2 v) =>
        v.sqrMagnitude > MinDistance * MinDistance
            ? v.normalized
            : Vector2.zero;

    public bool InRange(float distance) => distance <= Info.shootingRange;

    public bool IsAimedAt(Vector2 direction) =>
        Vector2.Angle(Forward, direction) <= Info.aimTolerance;

    // Fires every gun if the cooldown is over and it's aimed. True if it fired.
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

    // Thrust toward a world direction (length 0..1 = throttle). Adds the push
    // away from nearby guards so they don't stack on top of each other.
    public void Steer(Vector2 worldDirection)
    {
        Vector2 desired = worldDirection +
                          Separation() * Info.separationStrength;
        desired = AvoidStation(Vector2.ClampMagnitude(desired, 1f));

        Vector2 local = transform.InverseTransformDirection(desired);
        shipManager.Move(local);
    }

    // Approach to stopping distance, back off if too close, otherwise hold.
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

    // Turns the nose toward a direction (same turning as EnemyController).
    public void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < MinDistance * MinDistance)
        {
            return;
        }

        // -90 because the ship's nose is its local up.
        float targetAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float angleErr = Mathf.DeltaAngle(rb.rotation, targetAngle);

        // PID turning, the same as EnemyController.HandleRotation: it asks
        // the thrusters to turn instead of setting the spin directly.
        float maxAngAccel = shipManager.GetMaxAngleAccelDeg();
        float angBrakeScale = maxAngAccel > 0.1f ? 10f / Mathf.Sqrt(maxAngAccel) : 1f;
        float rotInput = _turnPID.CalculateVelocity(
            turnPID, Time.fixedDeltaTime, angleErr,
            rb.angularVelocity * angBrakeScale);

        shipManager.Rotate(rotInput);
    }

    // Inside the keep-out circle around the home station, the part of a move
    // that points into the station is turned sideways, so the guard slides
    // around the station instead of thrusting into its hull. Closer in, it
    // also gets a push outward.
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
