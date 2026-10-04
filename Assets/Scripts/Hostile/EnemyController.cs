using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class AIMovementStats
{
    public const float MaxWeaveOffset = 10f;
    public const float MinDistance = 0.001f;
    public const float MinSpeedSquared = 0.05f;
    public const float MinEnemyFacingDir = 0.0001f;
    public float speed = 4f;
    public float acceleration = 6f;
    public float stoppingDistance = 6f; // Stop approaching once this close
    public float retreatDistance = 3f; // Back away if closer than this
    public float turnSpeed = 4f; // How quickly the ship rotates
    public float driftAmount = 0.3f;
    public float weaveFrequency = 1.5f;
}

[Serializable]
public class AICombatStats
{
    //? [CB] Should the enemy just shoot any time they're able to?
    public float startTimeBtwShots = 1.5f;
    public float shootingRange = 8f;
    public float aimTolerance = 15f;
}

[RequireComponent(typeof(ShipManager))]
public class EnemyController : MonoBehaviour
{
    [SerializeField]
    private AIMovementStats movementStats;

    [SerializeField]
    private AICombatStats combatStats;

    private Transform _player;

    // TODO: make this a boolean value with a coroutine to handle if you can fire or not
    private float _timeBtwShots;
    private Vector3 _velocity;
    private float _weaveOffset;

    [SerializeField]
    private ShipManager shipManager;

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

        _timeBtwShots = combatStats.startTimeBtwShots;
        _weaveOffset = Random.value * AIMovementStats.MaxWeaveOffset;
    }

    private void Update()
    {
        // TODO: Make this a coroutine
        if (_player is null)
        {
            return;
        }

        Vector3 toPlayer = _player.position - transform.position;
        /*
         * It resets Z axis differences between player and enemy to zero
         * to keep movement and aiming on the XY plane and horizontal.
         * To ensure their height difference stays zero because
         * direction to player is recalculated every frame.
         */
        toPlayer.z = 0;
        float distance = toPlayer.magnitude;
        Vector3 dirToPlayer = distance > AIMovementStats.MinDistance
            ? toPlayer / distance
            : transform.up;

        HandleMovement(dirToPlayer, distance);
        HandleRotation(dirToPlayer, distance);
        HandleShooting(dirToPlayer, distance);
    }

    private void HandleMovement(Vector3 dirToPlayer, float distance)
    {
        Vector3 desiredVelocity = Vector3.zero;

        if (distance > movementStats.stoppingDistance)
        {
            desiredVelocity = dirToPlayer * movementStats.speed; // approach
        }
        else if (distance < movementStats.retreatDistance)
        {
            desiredVelocity = -dirToPlayer * movementStats.speed; // back away
        }

        if (desiredVelocity != Vector3.zero && movementStats.driftAmount > 0f)
        {
            Vector3 sideways = Vector3.Cross(Vector3.forward, dirToPlayer);
            float weave
                = Mathf.Sin(Time.time * movementStats.weaveFrequency +
                            _weaveOffset) *
                  movementStats.driftAmount;
            desiredVelocity += sideways * (weave * movementStats.speed);
            desiredVelocity
                = Vector3.ClampMagnitude(desiredVelocity, movementStats.speed);
        }

        _velocity = Vector3.MoveTowards(_velocity, desiredVelocity,
            movementStats.acceleration * Time.deltaTime);

        transform.position += _velocity * Time.deltaTime;
    }

    private void HandleRotation(Vector3 dirToPlayer, float distance)
    {
        Vector3 lookDir;
        if (distance <= movementStats.stoppingDistance ||
            _velocity.sqrMagnitude < AIMovementStats.MinSpeedSquared)
        {
            lookDir = dirToPlayer;
        }
        else
        {
            lookDir = _velocity.normalized;
        }

        if (lookDir.sqrMagnitude < AIMovementStats.MinEnemyFacingDir)
        {
            return;
        }

        Quaternion targetRotation
            = Quaternion.LookRotation(lookDir, Vector3.up);

        // [CB] TODO: Make this call shipManager.Rotate
        float turnAmount
            = 1f - Mathf.Exp(-movementStats.turnSpeed * Time.deltaTime);
        transform.rotation
            = Quaternion.Slerp(transform.rotation, targetRotation, turnAmount);
    }

    private void HandleShooting(Vector3 dirToPlayer, float distance)
    {
        _timeBtwShots -= Time.deltaTime;
        if (_timeBtwShots > 0f)
        {
            return;
        }

        if (distance > combatStats.shootingRange)
        {
            return;
        }

        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
        if (angleToPlayer > combatStats.aimTolerance)
        {
            return;
        }

        shipManager.Fire();
    }
}