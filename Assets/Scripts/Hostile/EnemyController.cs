using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.Hostile.Behaviors;
using Pulsar.Player;
using Pulsar.Ship;
using Pulsar.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ShipManager))]
public class EnemyController : MonoBehaviour
{
    public static event Action<EnemyController, int> EnemyDefeatedEvent;

    public static IReadOnlyCollection<EnemyController> ActiveEnemies =>
        _activeEnemies;

    [SerializeField]
    private EnemyInfo enemyInfo;

    [SerializeField]
    private ShipManager shipManager;

    private const float MaxWeaveOffset = 10f;
    private const float MinDistance = 0.001f;
    private const float TileHalfDiagonal = 0.7071f;
    private const float TurnGain = 4f;

    // Two far-apart rows of the noise, so sideways and in-out drift
    // don't move in lockstep.
    private const float SidewaysNoiseRow = 0f;
    private const float InOutNoiseRow = 57.3f;

    private static readonly HashSet<EnemyController> _activeEnemies = new();

    private readonly PIDController _moveXPID = new();
    private readonly PIDController _moveYPID = new();
    private readonly PIDController _turnPID = new();

    [SerializeField]
    private Rigidbody2D rb;

    [FormerlySerializedAs("_movePID")]
    [Header("PID Tuning")]
    [Tooltip("Movement positioning/breaking strength")]
    [AllowNesting]
    [SerializeField]
    private PIDParams movePID = new(1.0f, 1.2f, 0f);

    [Tooltip("Turning positioning/breaking strength")]
    [AllowNesting]
    [SerializeField]
    private PIDParams turnPID = new(0.08f, 0.04f, .001f);

    [Header("Debug (read only)")]
    [Tooltip("The combat behaviour this enemy kept, and its current state.")]
    [SerializeField]
    private string currentBehavior;

    // TODO: make this a boolean value with a coroutine to handle if you can fire or not
    private float _timeBtwShots;
    private float _weaveOffset;

    // The one Fighter/Bombard/Orbit this enemy kept, or null to fight the
    // old way (chase to stopping distance and hold).
    private EnemyCombatBehavior _behavior;

    public float Radius { get; private set; }

    // Read-only, so a station can check a prefab's PressureScore before spawning it.
    public EnemyInfo Info => enemyInfo;

    public Vector2 Position => rb.position;
    public Vector2 Forward => transform.up;

    private void Awake()
    {
        if (!rb)
        {
            Debug.LogError(
                $"Rigidbody of {gameObject.name} should have been auto-attached!");
            rb = GetComponent<Rigidbody2D>();
        }

        Radius = CalculateRadius(transform);
        // Smooth the ship and its tiles between physics steps so they
        // don't stutter on frames where physics doesn't run.
        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>())
        {
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        shipManager.ShipDestroyedEvent += HandleShipDestroyed;
        PickRandomBehavior();
    }

    // If this prefab carries combat behaviours, keeps one at random and
    // removes the rest, so the same prefab can be a Fighter one game and an
    // Orbit the next. With none, the enemy keeps its old chase-and-hold.
    private void PickRandomBehavior()
    {
        EnemyCombatBehavior[] options
            = GetComponents<EnemyCombatBehavior>();
        if (options.Length == 0)
        {
            currentBehavior = "None (chase and hold)";
            return;
        }

        _behavior = options[Random.Range(0, options.Length)];
        foreach (EnemyCombatBehavior option in options)
        {
            if (option != _behavior)
            {
                Destroy(option);
            }
        }

        _behavior.ResetCycle();
        currentBehavior = _behavior.CurrentStateName;
    }

    private void Start()
    {
        // TODO: Change this to use dependency injection.
        // In the future we could have a manager class that can get us a static player reference.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            throw new InvalidOperationException("Player not found");
        }

        _timeBtwShots = enemyInfo.startTimeBtwShots;
        _weaveOffset = Random.value * MaxWeaveOffset;
    }

    private void Update()
    {
        _timeBtwShots -= Time.deltaTime;
        if (_behavior)
        {
            // The behaviour does its own shooting (TryFire) in FixedUpdate.
            return;
        }

        GetPlayerDirection(out Vector3 dirToPlayer, out float distance);
        HandleShooting(dirToPlayer, distance);
    }

    // Movement runs on the physics clock so the tiles, which are joined
    // to the ship by physics, move in step with it instead of lagging behind.
    private void FixedUpdate()
    {
        if (_behavior)
        {
            _behavior.Tick(this, PlayerController.PlayerPosition,
                Time.fixedDeltaTime);
            currentBehavior = _behavior.CurrentStateName;
            return;
        }

        GetPlayerDirection(out Vector3 dirToPlayer, out float distance);
        HandleMovement(dirToPlayer, distance);
        HandleRotation(dirToPlayer);
    }

    private void OnEnable()
    {
        _activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        _activeEnemies.Remove(this);
    }

    private void OnDestroy()
    {
        shipManager.ShipDestroyedEvent -= HandleShipDestroyed;
    }

    // Puts a stray enemy back near the fight, stopped, as if it just spawned.
    public void Recall(Vector3 position)
    {
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = position;
        _moveXPID.Reset();
        _moveYPID.Reset();
        _turnPID.Reset();
    }

    private void HandleShipDestroyed(ShipManager obj)
    {
        //Game manager will use this for score
        EnemyDefeatedEvent?.Invoke(this, enemyInfo.PressureScore);
    }

    private void GetPlayerDirection(out Vector3 dirToPlayer, out float distance)
    {
        Vector3 toPlayer = PlayerController.PlayerPosition - transform.position;
        /*
         * It resets Z axis differences between player and enemy to zero
         * to keep movement and aiming on the XY plane and horizontal.
         * To ensure their height difference stays zero because
         * direction to player is recalculated every frame.
         */
        toPlayer.z = 0;
        distance = toPlayer.magnitude;
        dirToPlayer = distance > MinDistance
            ? toPlayer / distance
            : transform.up;
    }

    // How far the ship's furthest tile corner reaches from the ship's center.
    public static float CalculateRadius(Transform ship)
    {
        float radius = 0f;
        foreach (Tile tile in ship.GetComponentsInChildren<Tile>())
        {
            Vector3 offset
                = ship.InverseTransformPoint(tile.transform.position);
            offset.z = 0;
            radius = Mathf.Max(radius, offset.magnitude + TileHalfDiagonal);
        }

        return radius;
    }

    private void HandleMovement(Vector3 dirToPlayer, float distance)
    {
        Vector3 targetOffset = Vector3.zero;

        if (distance > enemyInfo.stoppingDistance)
        {
            targetOffset
                = dirToPlayer * (distance - enemyInfo.stoppingDistance);
        }
        else if (distance < enemyInfo.retreatDistance)
        {
            targetOffset
                = -dirToPlayer * (enemyInfo.retreatDistance - distance);
        }

        if (enemyInfo.driftAmount > 0f)
        {
            bool holdingPosition = targetOffset == Vector3.zero;
            targetOffset += CalculateDrift(dirToPlayer, holdingPosition);
        }

        targetOffset += CalculateSeparation() * enemyInfo.separationStrength;

        //Finds the position error in local space
        Vector2 localPosErr = transform.InverseTransformDirection(targetOffset);
        Vector2 localVel
            = transform.InverseTransformDirection(rb.linearVelocity);

        //How effective the brakes are based on the current speed
        Vector2 curBrakeStrength = new(-Mathf.Sign(localVel.x),
            -Mathf.Sign(localVel.y));
        //Get the brake strength by taking max accel in the opposite dir
        Vector2 maxBrake = shipManager.GetMaxLocalAccel(curBrakeStrength);

        float brakeScaleX = maxBrake.x > MinDistance
            ? 1f / Mathf.Sqrt(maxBrake.x)
            : 1f;
        float brakeScaleY = maxBrake.y > MinDistance
            ? 1f / Mathf.Sqrt(maxBrake.y)
            : 1f;

        float dt = Time.fixedDeltaTime;
        float inputX = _moveXPID.CalculateVelocity(
            movePID, dt, localPosErr.x, localVel.x * brakeScaleX);
        float inputY = _moveXPID.CalculateVelocity(
            movePID, dt, localPosErr.y, localVel.y * brakeScaleY);

        shipManager.Move(new Vector2(inputX, inputY));
    }

    // Smooth, rhythm-free wandering so enemies feel piloted, not on rails.
    // Perlin noise drifts up and down without repeating a fixed pattern, and
    // each enemy reads its own stretch of it (_weaveOffset), so no two move
    // alike. While holding position the ship also bobs closer and farther;
    // if it drifts out of its band, the chase or back-off pulls it back in.
    private Vector3 CalculateDrift(Vector3 dirToPlayer, bool holdingPosition)
    {
        float t = Time.time * enemyInfo.weaveFrequency + _weaveOffset;
        Vector3 sideways = Vector3.Cross(Vector3.forward, dirToPlayer);
        Vector3 drift = sideways * SignedNoise(t, SidewaysNoiseRow);

        if (holdingPosition)
        {
            drift += dirToPlayer * SignedNoise(t, InOutNoiseRow);
            drift *= enemyInfo.holdDrift;
        }

        return drift * enemyInfo.driftAmount;
    }

    // Perlin noise remapped from about 0..1 to -1..1.
    private static float SignedNoise(float x, float row)
    {
        return Mathf.Clamp(Mathf.PerlinNoise(x, row) * 2f - 1f, -1f, 1f);
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 push = Vector3.zero;

        foreach (EnemyController other in ActiveEnemies)
        {
            if (other == this)
            {
                continue;
            }


            Vector3 offset = transform.position - other.transform.position;
            float range = Radius + other.Radius + enemyInfo.separationPadding;
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

    private void HandleRotation(Vector3 dirToPlayer)
    {
        float targetAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x)
            * Mathf.Rad2Deg - 90f;

        float angleErr = Mathf.DeltaAngle(rb.rotation, targetAngle);

        float maxAngAccel = shipManager.GetMaxAngleAccelDeg();
        float angBrakeScale = maxAngAccel > 0.1f
            ? 10f / Mathf.Sqrt(maxAngAccel)
            : 1f;

        float rotInput = _turnPID.CalculateVelocity(
            turnPID, Time.fixedDeltaTime, angleErr,
            rb.angularVelocity * angBrakeScale);

        shipManager.Rotate(rotInput);
    }

    private void HandleShooting(Vector3 dirToPlayer, float distance)
    {
        // (_timeBtwShots counts down in Update.)
        if (_timeBtwShots > 0f)
        {
            return;
        }

        if (distance > enemyInfo.shootingRange)
        {
            return;
        }

        float angleToPlayer = Vector3.Angle(transform.up, dirToPlayer);
        if (angleToPlayer > enemyInfo.aimTolerance)
        {
            return;
        }

        shipManager.Fire();
        _timeBtwShots = enemyInfo.startTimeBtwShots;
    }

    // Tools for the combat behaviours (Fighter, Bombard, Orbit).

    public static Vector2 Direction(Vector2 v) =>
        v.sqrMagnitude > MinDistance * MinDistance
            ? v.normalized
            : Vector2.zero;

    public bool InRange(float distance) =>
        distance <= enemyInfo.shootingRange;

    public bool IsAimedAt(Vector2 direction) =>
        Vector2.Angle(Forward, direction) <= enemyInfo.aimTolerance;

    // Fires every gun if the cooldown is over and it's aimed. True if it fired.
    public bool TryFire(Vector2 toTarget)
    {
        if (_timeBtwShots > 0f || !IsAimedAt(toTarget))
        {
            return false;
        }

        shipManager.Fire();
        _timeBtwShots = enemyInfo.startTimeBtwShots;
        return true;
    }

    // Thrust toward a world direction (length 0..1 = throttle), plus the
    // push away from nearby enemies so they don't stack on top of each other.
    public void Steer(Vector2 worldDirection)
    {
        Vector2 separation = CalculateSeparation();
        Vector2 desired = Vector2.ClampMagnitude(
            worldDirection + separation * enemyInfo.separationStrength, 1f);
        Vector2 local = transform.InverseTransformDirection(desired);
        shipManager.Move(local);
    }

    // Approach to stopping distance, back off if too close, otherwise hold.
    public void KeepDistance(Vector2 toTarget, float distance)
    {
        Vector2 direction = Direction(toTarget);
        if (distance > enemyInfo.stoppingDistance)
        {
            Steer(direction);
        }
        else if (distance < enemyInfo.retreatDistance)
        {
            Steer(-direction);
        }
        else
        {
            Steer(Vector2.zero);
        }
    }

    // Turns the nose toward a direction with the same PID turning as the
    // old chase (HandleRotation).
    public void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < MinDistance * MinDistance)
        {
            return;
        }

        HandleRotation(direction);
    }
}
