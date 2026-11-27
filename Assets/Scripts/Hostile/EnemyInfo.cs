using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Combat")]
    public float startTimeBtwShots = 1.5f;

    public float shootingRange = 8f;
    public float aimTolerance = 15f;

    [Header("Separation")]
    public float
        separationPadding = 1f; // Extra gap kept between the edges of two ships

    public float separationStrength = 6f;

    [Header("Scoring & Pressure")]
    [Tooltip("Points for killing this enemy, and how much pressure it puts on its station's maxPressure.")]
    [Min(1)]
    [FormerlySerializedAs("PointsOnKill")]
    public int PressureScore = 10;
}
