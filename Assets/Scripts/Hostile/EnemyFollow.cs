using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyFollow : MonoBehaviour
{
    private const float MaxWeaveOffset = 10f;
    private const float MinDistance = 0.001f;
    private const float MinSpeedSquared = 0.05f;
    private const float MinEnemyFacingDir = 0.0001f;

    [SerializeField]
    private float speed = 4f;

    [SerializeField]
    private float acceleration = 6f;

    [SerializeField]
    private float stoppingDistance = 6f; // Stop approaching once this close

    [SerializeField]
    private float retreatDistance = 3f; // Back away if closer than this

    [SerializeField]
    private float turnSpeed = 4f; // How quickly the ship rotates

    [SerializeField]
    private float driftAmount = 0.3f;

    [SerializeField]
    private float weaveFrequency = 1.5f;

    [SerializeField]
    private GameObject projectile;

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private float startTimeBtwShots = 1.5f;

    [SerializeField]
    private float shootingRange = 8f;

    [SerializeField]
    private float aimTolerance = 15f;

    private Transform player;
        // TODO: make this a boolean value with a coroutine to handle if you can fire or not
        private float timeBtwShots;
    private Vector3 velocity;
    private float weaveOffset;

    private void Start()
    {
        // TODO: Change this to use dependency injection.
        // In the future we could have a manager class that can get us a static player reference.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            throw new InvalidOperationException("Player not found");
        }

        player = playerObj.transform;

        timeBtwShots = startTimeBtwShots;
        weaveOffset = Random.value * MaxWeaveOffset;
    }

    private void Update()
    {
        // TODO: Make this a coroutine
        if (player == null)
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        /*
         * It resets Y axis differences between player and enemy to zero
         * to keep movement and aiming on the XZ plane and horizontal.
         * To ensure their height difference stays zero because direction to player is recalculated every frame.
         */
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        Vector3 dirToPlayer = distance > MinDistance
            ? toPlayer / distance
            : transform.forward;

        HandleMovement(dirToPlayer, distance);
        HandleRotation(dirToPlayer, distance);
        HandleShooting(dirToPlayer, distance);
    }

    private void HandleMovement(Vector3 dirToPlayer, float distance)
    {
        Vector3 desiredVelocity = Vector3.zero;

        if (distance > stoppingDistance)
        {
            desiredVelocity = dirToPlayer * speed; // approach
        }
        else if (distance < retreatDistance)
        {
            desiredVelocity = -dirToPlayer * speed; // back away
        }


        if (desiredVelocity != Vector3.zero && driftAmount > 0f)
        {
            Vector3 sideways = Vector3.Cross(Vector3.up, dirToPlayer);
            float weave = Mathf.Sin(Time.time * weaveFrequency + weaveOffset) *
                          driftAmount;
            desiredVelocity += sideways * (weave * speed);
            desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, speed);
        }

        velocity = Vector3.MoveTowards(velocity, desiredVelocity,
            acceleration * Time.deltaTime);

        transform.position += velocity * Time.deltaTime;
    }

    private void HandleRotation(Vector3 dirToPlayer, float distance)
    {
        Vector3 lookDir;
        if (distance <= stoppingDistance ||
            velocity.sqrMagnitude < MinSpeedSquared)
        {
            lookDir = dirToPlayer;
        }
        else
        {
            lookDir = velocity.normalized;
        }

        if (lookDir.sqrMagnitude < MinEnemyFacingDir)
        {
            return;
        }

        Quaternion targetRotation
            = Quaternion.LookRotation(lookDir, Vector3.up);

        float turnAmount = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
        transform.rotation
            = Quaternion.Slerp(transform.rotation, targetRotation, turnAmount);
    }

    private void HandleShooting(Vector3 dirToPlayer, float distance)
    {
        timeBtwShots -= Time.deltaTime;

        if (timeBtwShots > 0f || projectile == null)
        {
            return;
        }

        if (distance > shootingRange)
        {
            return;
        }

        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
        if (angleToPlayer > aimTolerance)
        {
            return;
        }

        if (firePoint == null)
        {
            throw new InvalidOperationException(
                "FirePoint is missing. Assign it in the Inspector.");
        }

        Instantiate(projectile, firePoint.position,
            firePoint.rotation);
        timeBtwShots = startTimeBtwShots;
    }
}