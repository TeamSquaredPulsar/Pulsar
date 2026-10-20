using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ShipManager))]
public class EnemyController : MonoBehaviour
{
    private const float MaxWeaveOffset = 10f;
    private const float MinDistance = 0.001f;
    private const float MinSpeedSquared = 0.05f;
    private const float MinEnemyFacingDir = 0.0001f;

    [SerializeField]
    private EnemyInfoSO enemyInfo;

    [SerializeField]
    private ShipManager shipManager;

    private Transform _player;

    // TODO: make this a boolean value with a coroutine to handle if you can fire or not
    private float _timeBtwShots;
    private Vector3 _velocity;
    private float _weaveOffset;

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

        Vector3 toPlayer = _player.position - transform.position;
        /*
         * It resets Z axis differences between player and enemy to zero
         * to keep movement and aiming on the XY plane and horizontal.
         * To ensure their height difference stays zero because
         * direction to player is recalculated every frame.
         */
        toPlayer.z = 0;
        float distance = toPlayer.magnitude;
        Vector3 dirToPlayer = distance > MinDistance
            ? toPlayer / distance
            : transform.up;

        HandleMovement(dirToPlayer, distance);
        HandleRotation(dirToPlayer, distance);
        HandleShooting(dirToPlayer, distance);
    }

    private void HandleMovement(Vector3 dirToPlayer, float distance)
    {
        Vector3 desiredVelocity = Vector3.zero;

        if (distance > enemyInfo.stoppingDistance)
        {
            desiredVelocity = dirToPlayer * enemyInfo.speed; // approach
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

        _velocity = Vector3.MoveTowards(_velocity, desiredVelocity,
            enemyInfo.acceleration * Time.deltaTime);

        transform.position += _velocity * Time.deltaTime;
    }

    private void HandleRotation(Vector3 dirToPlayer, float distance)
    {
        Vector3 lookDir;
        if (distance <= enemyInfo.stoppingDistance ||
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
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);

        // [CB] TODO: Make this call shipManager.Rotate
        float turnAmount
            = 1f - Mathf.Exp(-enemyInfo.turnSpeed * Time.deltaTime);
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
