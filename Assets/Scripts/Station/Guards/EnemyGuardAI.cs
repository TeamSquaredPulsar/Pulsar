using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.Ship;
using Pulsar.Utilities;
using UnityEngine;

namespace Pulsar.Stations.Guards
{
/// <summary>
/// The brain of a station guard. Guards only patrol: they circle their home
/// station, never chase the player and never shoot. Movement and turning use
/// PID controllers (like EnemyController) through ShipManager.Move and Rotate.
/// (Fighter, Bombard and Orbit now belong to the wave enemies, in
/// Hostile/Behaviors, driven by EnemyController.)
/// </summary>
[RequireComponent(typeof(ShipManager), typeof(Rigidbody2D))]
public class EnemyGuardAI : MonoBehaviour
{
    private const float MinDistance = 0.001f;

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

    [Tooltip(
        "The nose aims at a spot this many degrees further along the ring " +
        "(a look-ahead point), so the heading stays steady instead of " +
        "swinging around the moving patrol point.")]
    [Range(0f, 90f)]
    [SerializeField]
    private float lookAheadDegrees = 25f;

    [Header("Station avoidance")]
    [Tooltip("Extra space kept between this guard and its station's outline.")]
    [Min(0f)]
    [SerializeField]
    private float stationGap = 1.5f;

    private SpaceStation _home;
    private float _patrolRadius;
    private float _patrolAngle;
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

    // This guard was destroyed. Its station takes it off its list. (Guards give no score.)
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

        // Aim the nose at a look-ahead point further along the ring, not at
        // the patrol point itself. Close to the patrol point, the direction to
        // it flips around every time the guard overshoots, which made the
        // nose swing and the thrusters flicker. The look-ahead point is far
        // enough away that the heading only changes smoothly.
        // (Sign: ahead in whichever direction the guards circle.)
        float aheadRadians = (_patrolAngle +
                              lookAheadDegrees * Mathf.Sign(patrolDegreesPerSecond)) *
                             Mathf.Deg2Rad;
        Vector2 aheadPoint = (Vector2)_home.transform.position +
                             new Vector2(Mathf.Cos(aheadRadians),
                                 Mathf.Sin(aheadRadians)) * _patrolRadius;
        Face(aheadPoint - Position);
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

    private static Vector2 Direction(Vector2 v) =>
        v.sqrMagnitude > MinDistance * MinDistance
            ? v.normalized
            : Vector2.zero;

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

        // Guards also make way for enemies (enemies don't dodge guards back:
        // their code can't see the station assembly).
        foreach (EnemyController enemy in EnemyController.ActiveEnemies)
        {
            Vector2 offset = Position - (Vector2)enemy.transform.position;
            float range = Radius + enemy.Radius + Info.separationPadding;
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
        Defeated?.Invoke(this);
    }
}
}
