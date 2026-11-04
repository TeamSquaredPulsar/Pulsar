using System;
using System.Collections.Generic;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Player;
using Pulsar.Ship;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ShipManager))]
public class EnemyController : MonoBehaviour
{
    public static event Action<EnemyController, int> EnemyDefeatedEvent;

    public static IReadOnlyCollection<EnemyController> ActiveEnemies =>
        _activeEnemies;

    [SerializeField]
    private EnemyInfoSO enemyInfo;

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

    [GetComponent, SerializeField,]
    private Rigidbody2D rb;

    // TODO: make this a boolean value with a coroutine to handle if you can fire or not
    private float _timeBtwShots;
    private float _weaveOffset;

    public float Radius { get; private set; }

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
        GetPlayerDirection(out Vector3 dirToPlayer, out float distance);
        HandleShooting(dirToPlayer, distance);
    }

    // Movement runs on the physics clock so the tiles, which are joined
    // to the ship by physics, move in step with it instead of lagging behind.
    private void FixedUpdate()
    {
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
    }

    private void HandleShipDestroyed(ShipManager obj)
    {
        //Game manager will use this for score
        EnemyDefeatedEvent?.Invoke(this, enemyInfo.PointsOnKill);
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
        Vector3 desiredDir = Vector3.zero;

        if (distance > enemyInfo.stoppingDistance)
        {
            desiredDir = dirToPlayer;
        }
        else if (distance < enemyInfo.retreatDistance)
        {
            desiredDir = -dirToPlayer;
        }

        if (enemyInfo.driftAmount > 0f)
        {
            bool holdingPosition = desiredDir == Vector3.zero;
            desiredDir += CalculateDrift(dirToPlayer, holdingPosition);
        }

        desiredDir += CalculateSeparation() * enemyInfo.separationStrength;

        desiredDir = Vector3.ClampMagnitude(desiredDir, 1f) *
                     Mathf.Clamp01(enemyInfo.speed);

        Vector2 localDir = transform.InverseTransformDirection(desiredDir);
        shipManager.Move(localDir);
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
        //TODO Replace this with a PID controller that turns through
        //  shipManager.Rotate instead of setting angularVelocity directly
        //-90f corrects rotation ONLY for this specific Band-Aid
        float targetAngle = Mathf.Atan2(
            dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f;

        float angleDiff = Mathf.DeltaAngle(rb.rotation, targetAngle);

        float desiredVelocity = angleDiff * TurnGain;

        rb.angularVelocity = Mathf.Clamp(desiredVelocity,
            -enemyInfo.turnSpeed, enemyInfo.turnSpeed);
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