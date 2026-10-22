using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ShipManager))]
public class EnemyController : MonoBehaviour
{
    private const float MaxWeaveOffset = 10f;
    private const float MinDistance = 0.001f;
    private const float TileHalfDiagonal = 0.7071f;
    private const float TurnGain = 4f;
    private const float SpinResponse = 30f;

    private static readonly HashSet<EnemyController> _activeEnemies = new();

    [SerializeField]
    private EnemyInfoSO enemyInfo;

    [SerializeField]
    private ShipManager shipManager;

    private Transform _player;
    private Rigidbody2D _rb;

    // TODO: make this a boolean value with a coroutine to handle if you can fire or not
    private float _timeBtwShots;
    private float _weaveOffset;

    public float Radius { get; private set; }

    public static IReadOnlyCollection<EnemyController> ActiveEnemies =>
        _activeEnemies;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        Radius = CalculateRadius(transform);
        _rb.centerOfMass = CalculateShipCenterOfMass();
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

    private Vector2 CalculateShipCenterOfMass()
    {
        Vector2 weightedSum = Vector2.zero;
        float totalMass = 0f;
        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>())
        {
            Vector2 localPos
                = transform.InverseTransformPoint(body.transform.position);
            weightedSum += localPos * body.mass;
            totalMass += body.mass;
        }

        return weightedSum / totalMass;
    }

    private void HandleMovement(Vector3 dirToPlayer, float distance)
    {
        Vector3 desiredDir = Vector3.zero;

        if (distance > enemyInfo.stoppingDistance)
        {
            desiredDir = dirToPlayer;
        }
        else if (distance < enemyInfo.retreatDistance)
        {
            desiredDir = -dirToPlayer;
        }

        if (desiredDir != Vector3.zero && enemyInfo.driftAmount > 0f)
        {
            Vector3 sideways = Vector3.Cross(Vector3.forward, dirToPlayer);
            float weave = Mathf.Sin(Time.time * enemyInfo.weaveFrequency +
                                    _weaveOffset) * enemyInfo.driftAmount;
            desiredDir += sideways * weave;
        }

        desiredDir += CalculateSeparation() * enemyInfo.separationStrength;

        desiredDir = Vector3.ClampMagnitude(desiredDir, 1f) *
                     Mathf.Clamp01(enemyInfo.speed);

        Vector2 localDir = transform.InverseTransformDirection(desiredDir);
        shipManager.Move(localDir);
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
        float targetAngle
            = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f;
        float angleDiff = Mathf.DeltaAngle(_rb.rotation, targetAngle);

        float targetSpin = Mathf.Clamp(angleDiff * TurnGain,
            -enemyInfo.turnSpeed, enemyInfo.turnSpeed);

        float rotInput = Mathf.Clamp(
            (targetSpin - _rb.angularVelocity) / SpinResponse, -1f, 1f);

        shipManager.Rotate(-rotInput);
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