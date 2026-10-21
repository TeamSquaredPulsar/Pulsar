using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyInfo", menuName = "Pulsar/Enemy Info")]
public class EnemyInfoSO : ScriptableObject
{
    [Header("Movement")]
    public float speed = 4f;

    public float acceleration = 6f;
    public float stoppingDistance = 6f; // Stop approaching once this close
    public float retreatDistance = 3f; // Back away if closer than this
    public float turnSpeed = 4f; // How quickly the ship rotates
    public float driftAmount = 0.3f;
    public float weaveFrequency = 1.5f;

    [Header("Combat")]
    public float startTimeBtwShots = 1.5f;

    public float shootingRange = 8f;
    public float aimTolerance = 15f;

    [Header("Separation")]
    public float
        separationPadding = 1f; // Extra gap kept between the edges of two ships

    public float separationStrength = 6f;
}