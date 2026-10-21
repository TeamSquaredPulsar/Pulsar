using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ShipManager))]
public class EnemyController : MonoBehaviour
{
    private const float MaxWeaveOffset = 10f;
    private const float MinDistance = 0.001f;
    private const float MinSpeedSquared = 0.05f;
    private const float MinEnemyFacingDir = 0.0001f;
    private const float TileHalfDiagonal = 0.7071f;
    private const float SlowDownRange = 2f;

    private static readonly HashSet<EnemyController> _activeEnemies = new();

    [SerializeField]
    private EnemyInfoSO enemyInfo;

    [SerializeField]
    private ShipManager shipManager;

    private Transform _player;
    private Rigidbody2D _rb;

    // TODO: make this a boolean value with a coroutine to handle if you can fire or not
    private float _timeBtwShots;
    private Vector3 _velocity;
    private float _weaveOffset;

    public float Radius { get; private set; }

    public static IReadOnlyCollection<EnemyController> ActiveEnemies =>
        _activeEnemies;

    private void Awake()
    {
        Radius = CalculateRadius(transform);
        _rb = GetComponent<Rigidbody2D>();

        // Smooth the ship and its tiles between physics steps so they
        // don't stutter on frames where physics doesn't run.
        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>())
        {
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
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

        _player = playerObj.transform;

        _timeBtwShots = enemyInfo.startTimeBtwShots;
        _weaveOffset = Random.value * MaxWeaveOffset;
    }

    private void Update()
    {
        // TODO: Make this a coroutine
        if (_player is null)
        {
            return;
        }

        GetPlayerDirection(out Vector3 dirToPlayer, out float distance);
        HandleShooting(dirToPlayer, distance);
    }

    // Movement runs on the physics clock so the tiles, which are joined
    // to the ship by physics, move in step with it instead of lagging behind.
    private void FixedUpdate()
    {
        if (_player is null)
        {
            return;
        }

        GetPlayerDirection(out Vector3 dirToPlayer, out float distance);
        HandleMovement(dirToPlayer, distance);
        HandleRotation(dirToPlayer, distance);
    }

    private void OnEnable()
    {
        _activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        _activeEnemies.Remove(this);
    }

    private void GetPlayerDirection(out Vector3 dirToPlayer, out float distance)
    {
        Vector3 toPlayer = _player.position - transform.position;
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
        Vector3 desiredVelocity = Vector3.zero;

        if (distance > enemyInfo.stoppingDistance)
        {
            // Ease off over the last stretch instead of braking all at once.
            float slowDown = Mathf.Clamp01(
                (distance - enemyInfo.stoppingDistance) / SlowDownRange);
            desiredVelocity
                = dirToPlayer * (enemyInfo.speed * slowDown); // approach
        }
        else if (distance < enemyInfo.retreatDistance)
        {
            desiredVelocity = -dirToPlayer * enemyInfo.speed; // back away
        }

        if (desiredVelocity != Vector3.zero && enemyInfo.driftAmount > 0f)
        {
            Vector3 sideways = Vector3.Cross(Vector3.forward, dirToPlayer);
            float weave
                = Mathf.Sin(Time.time * enemyInfo.weaveFrequency +
                            _weaveOffset) *
                  enemyInfo.driftAmount;
            desiredVelocity += sideways * (weave * enemyInfo.speed);
            desiredVelocity
                = Vector3.ClampMagnitude(desiredVelocity, enemyInfo.speed);
        }

        desiredVelocity += CalculateSeparation() * enemyInfo.separationStrength;
        desiredVelocity
            = Vector3.ClampMagnitude(desiredVelocity, enemyInfo.speed);

        _velocity = Vector3.MoveTowards(_velocity, desiredVelocity,
            enemyInfo.acceleration * Time.deltaTime);

        _rb.MovePosition(_rb.position + (Vector2)(_velocity * Time.deltaTime));
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
            float distance = offset.magnitude;
            float range = Radius + other.Radius + enemyInfo.separationPadding;

            if (distance < MinDistance ||
                distance >= range)
            {
                continue;
            }

            push += offset.normalized *
                    (1f - distance / range);
        }

        return push;
    }

    private void HandleRotation(Vector3 dirToPlayer, float distance)
    {
        Vector3 lookDir;
        if (distance <= enemyInfo.shootingRange ||
            _velocity.sqrMagnitude < MinSpeedSquared)
        {
            lookDir = dirToPlayer;
        }
        else
        {
            lookDir = _velocity.normalized;
        }

        if (lookDir.sqrMagnitude < MinEnemyFacingDir)
        {
            return;
        }

        // Turn around Z so the ship's local up direction faces the target.
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;

        // [CB] TODO: Make this call shipManager.Rotate
        float turnAmount
            = 1f - Mathf.Exp(-enemyInfo.turnSpeed * Time.deltaTime);
        _rb.MoveRotation(Mathf.LerpAngle(_rb.rotation, angle, turnAmount));
    }

    private void HandleShooting(Vector3 dirToPlayer, float distance)
    {
        _timeBtwShots -= Time.deltaTime;
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
}