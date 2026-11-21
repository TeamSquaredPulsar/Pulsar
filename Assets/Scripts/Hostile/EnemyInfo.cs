using Pulsar.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyInfo", menuName = "Pulsar/Enemy Info")]
public class EnemyInfo : ScriptableObject
{
    public float stoppingDistance = 6f; // Stop approaching once this close
    public float retreatDistance = 3f; // Back away if closer than this

    public float
        driftAmount
            = 0.3f; // How strongly the ship wanders off its straight path (0 = never)

    public float
        weaveFrequency = 1.5f; // How quickly the wander changes direction

    [Range(0f, 1f)]
    public float
        holdDrift
            = 0.5f; // Share of the drift kept while holding position (0 = sits still)

    [Header("PID Tuning")]
    [Tooltip("Movement positioning/breaking strength")]
    public PIDParams movePID = new(1.0f, 1.2f, 0f, 1f);

    [Tooltip("Turning positioning/breaking strength")]
    public PIDParams turnPID = new(0.08f, 0.04f, 0f, 1f);


    [Header("Combat")]
    public float startTimeBtwShots = 1.5f;

    public float shootingRange = 8f;
    public float aimTolerance = 15f;

    [Header("Separation")]
    public float
        separationPadding = 1f; // Extra gap kept between the edges of two ships

    public float separationStrength = 6f;

    [Header("Scoring")]
    [Min(1)]
    public int PointsOnKill = 10;
}
