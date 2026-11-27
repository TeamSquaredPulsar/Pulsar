using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyInfo", menuName = "Pulsar/Enemy Info")]
public class EnemyInfo : ScriptableObject
{
    public float stoppingDistance = 6f; // Stop approaching once this close
    public float retreatDistance = 3f; // Back away if closer than this

    public float
        driftAmount
            = 3f; // How strongly the ship wanders off its straight path (0 = never)

    public float
        weaveFrequency = 1.5f; // How quickly the wander changes direction

    [Range(0f, 1f)]
    public float
        holdDrift
            = 0.5f; // Share of the drift kept while holding position (0 = sits still)

    [Range(0f, 1f)]
    public float turnEfficiency = .50f;

    [Header("Combat")]
    public float startTimeBtwShots = 1.5f;

    public float shootingRange = 8f;
    public float rangeVariance = 3f;
    public float aimTolerance = 15f;
    public float maxAimTrackSpeed = 110f;

    [Header("Separation")]
    public float
        separationPadding = 1f; // Extra gap kept between the edges of two ships

    public float separationStrength = 6f;

    [Header("Scoring")]
    [Min(1)]
    public int PointsOnKill = 10;
}
