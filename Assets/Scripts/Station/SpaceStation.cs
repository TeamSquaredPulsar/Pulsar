using System.Collections;
using System.Collections.Generic;
using Pulsar.Hostile;
using Pulsar.Ship;
using Pulsar.Stations.Guards;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Stations
{
/// <summary>
/// One script for every station. The station's StationProfile says what's
/// different. When the station is enabled (its scene loads), it spawns each of
/// the profile's Guard Prefabs once, spread evenly on a ring around itself.
/// Guards only circle the station; killed guards don't come back.
///
/// While WaveManager has it switched on (StartSpawning / StopSpawning), it
/// also sends enemies at the player, topping them up under the profile's
/// MaxPressure: each living enemy counts its PressureScore, and the station
/// only spawns one whose score fits the room left. The list of living enemies
/// is the truth; the current pressure is always added up from it.
///
/// Its guards don't follow the game's state: OnEnable spawns them, and OnDisable
/// (which runs when its scene unloads) clears its guards and enemies. So it
/// works the same in test scenes and in the real game.
/// </summary>
public class SpaceStation : MonoBehaviour, IWaveEnemySource
{
    [SerializeField]
    private StationProfile profile;

    [Tooltip(
        "How far out the guards spawn and patrol, as a multiple of the station's own radius.")]
    [SerializeField]
    private float ringScale = 1.8f;

    [Tooltip(
        "How far outside the guard ring enemies appear, so they don't spawn in the guards' lane. Only for a station with no bay.")]
    [Min(0f)]
    [SerializeField]
    private float enemySpawnGap = 3f;

    [Tooltip(
        "Extra empty space needed around a new enemy's spawn spot. A spot " +
        "with any ship, tile or station part inside it is skipped.")]
    [Min(0f)]
    [SerializeField]
    private float spawnClearance = 1f;

    [Tooltip("Gap between the bay's opening and a new enemy's hull.")]
    [Min(0f)]
    [SerializeField]
    private float bayGap = 0.25f;

    [Tooltip("Seconds a new enemy flies straight out of the bay before its AI takes over.")]
    [Min(0f)]
    [SerializeField]
    private float launchSeconds = 1f;

    // How many random spots a spawn tries before giving up until the next tick.
    private const int MaxSpawnAttempts = 8;

    // This station's launch bays (none = enemies appear on the spawn ring).
    private StationBay[] _bays = System.Array.Empty<StationBay>();

    // Reused for the "is this spot empty?" check (no allocations per spawn).
    private readonly List<Collider2D> _spotHits = new();

    [Header("Debug (read only)")]
    [SerializeField]
    private int currentPressure;

    // The guards this station spawned that are still alive.
    private readonly List<EnemyGuardAI> spawnedGuards = new();

    // The enemies this station sent that are still alive.
    private readonly List<EnemyController> spawnedEnemies = new();

    private Coroutine _spawnRoutine;

    public StationProfile Profile => profile;

    // How far the station's furthest tile reaches (measured when it starts defending).
    public float Radius { get; private set; }
    public int CurrentPressure => currentPressure;
    public bool IsSpawning => _spawnRoutine != null;

    // Enemies this station sent that are still alive (WaveManager waits for 0).
    public int AliveEnemies
    {
        get
        {
            RecalculatePressure();
            return spawnedEnemies.Count;
        }
    }

    private void OnEnable()
    {
        EnemyController.EnemyDefeatedEvent += HandleEnemyDefeated;
        BeginDefense();
    }

    private void OnDisable()
    {
        EnemyController.EnemyDefeatedEvent -= HandleEnemyDefeated;
        ResetStation();
    }

    // Starts the station over with fresh guards. Enemies only come once
    // WaveManager calls StartSpawning.
    public void BeginDefense()
    {
        ResetStation();
        if (!profile)
        {
            Debug.LogError(
                $"{name} has no StationProfile, so it can't spawn guards.");
            return;
        }

        Radius = CalculateRadius();
        _bays = GetComponentsInChildren<StationBay>();
        SpawnGuards();
    }

    // Removes this station's guards and enemies and stops it (OnDisable, and before a restart).
    public void ResetStation()
    {
        foreach (EnemyGuardAI guard in spawnedGuards)
        {
            if (guard)
            {
                guard.Defeated -= HandleGuardDefeated;
                Destroy(guard.gameObject);
            }
        }

        spawnedGuards.Clear();

        StopSpawning();
        foreach (EnemyController enemy in spawnedEnemies)
        {
            if (enemy)
            {
                Destroy(enemy.gameObject);
            }
        }

        spawnedEnemies.Clear();
        currentPressure = 0;
    }

    // Each Guard Prefab spawns once, spread evenly around the ring, nose
    // pointing along the ring the way it will patrol.
    private void SpawnGuards()
    {
        EnemyGuardAI[] prefabs = profile.GuardPrefabs;
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning($"{name}'s profile has no Guard Prefabs, so it has no guards.");
            return;
        }

        Vector2 centre = transform.position;
        float ringRadius = Radius * ringScale;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (!prefabs[i])
            {
                continue;
            }

            float angle = startAngle + i * 360f / prefabs.Length;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 position = centre + new Vector2(Mathf.Cos(radians),
                Mathf.Sin(radians)) * ringRadius;
            // Nose along the ring: the ship's nose is its local up, so
            // "angle" itself points it 90 degrees round from the centre.
            EnemyGuardAI guard = Instantiate(prefabs[i], position,
                Quaternion.Euler(0f, 0f, angle));
            guard.name = $"{name} {prefabs[i].name}";
            StationTintable.TintAll(guard.gameObject, profile.StationColour);
            guard.Init(this, profile.GuardInfo, ringRadius, angle);
            guard.Defeated += HandleGuardDefeated;
            spawnedGuards.Add(guard);
        }
    }

    // A killed guard just leaves the list; nothing respawns.
    private void HandleGuardDefeated(EnemyGuardAI guard)
    {
        guard.Defeated -= HandleGuardDefeated;
        spawnedGuards.Remove(guard);
    }

    // Starts sending enemies, topping them up under MaxPressure.
    public void StartSpawning()
    {
        if (_spawnRoutine != null || !profile)
        {
            return;
        }

        if (profile.EnemyPrefabs == null || profile.EnemyPrefabs.Length == 0)
        {
            Debug.LogWarning(
                $"{name}'s profile has no Enemy Prefabs, so it sends no enemies.");
            return;
        }

        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    // Stops sending new enemies. The ones already out keep fighting.
    public void StopSpawning()
    {
        if (_spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(_spawnRoutine);
        _spawnRoutine = null;
    }

    private IEnumerator SpawnLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(profile.SecondsBetweenSpawns);
        while (true)
        {
            TrySpawnEnemy();
            yield return wait;
        }
    }

    // Spawns one enemy if one fits the room left under MaxPressure.
    private void TrySpawnEnemy()
    {
        RecalculatePressure();
        int room = profile.MaxPressure - currentPressure;

        List<EnemyController> fits = new();
        foreach (EnemyController prefab in profile.EnemyPrefabs)
        {
            if (prefab && prefab.Info && prefab.Info.PressureScore <= room)
            {
                fits.Add(prefab);
            }
        }

        if (fits.Count == 0)
        {
            return;
        }

        EnemyController chosen = fits[Random.Range(0, fits.Count)];

        // Comes out of a launch bay, nose pointing out, and flies straight out
        // for launchSeconds. A station with no bay uses the old spawn: just
        // outside the guard ring at a random angle. Either way the spot has
        // to be empty (two ships that appear inside each other get stuck);
        // no empty spot = skip this tick, try next time.
        Vector2 position;
        Vector2 outward;
        StationBay bay = null;
        if (_bays.Length > 0)
        {
            if (!TryFindBaySpot(chosen, out bay, out position))
            {
                return;
            }

            outward = bay.Direction;
        }
        else
        {
            if (!TryFindSpawnSpot(chosen, out position, out float angle))
            {
                return;
            }

            float radians = angle * Mathf.Deg2Rad;
            outward = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        Quaternion facing = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg - 90f);

        EnemyController enemy = Instantiate(chosen, position, facing);
        StationTintable.TintAll(enemy.gameObject, profile.StationColour);
        if (bay)
        {
            enemy.Launch(outward, launchSeconds);
        }

        spawnedEnemies.Add(enemy);
        RecalculatePressure();
    }

    // A bay (tried in random order) whose opening has nothing solid in front
    // of it for the new enemy. False if every bay is blocked.
    private bool TryFindBaySpot(EnemyController prefab, out StationBay bay, out Vector2 position)
    {
        float shipRadius = EnemyController.CalculateRadius(prefab.transform);
        float checkRadius = shipRadius + spawnClearance;
        ContactFilter2D solidOnly = new() { useTriggers = false };

        int start = Random.Range(0, _bays.Length);
        for (int i = 0; i < _bays.Length; i++)
        {
            bay = _bays[(start + i) % _bays.Length];
            if (!bay)
            {
                continue;
            }

            position = bay.Mouth(shipRadius, bayGap);
            Physics2D.OverlapCircle(position, checkRadius, solidOnly, _spotHits);
            if (!HitsAShip(_spotHits))
            {
                return true;
            }
        }

        bay = null;
        position = default;
        return false;
    }

    // A random spot on the spawn ring with nothing solid inside the new
    // enemy's radius (plus spawnClearance). False if every try was blocked.
    private bool TryFindSpawnSpot(EnemyController prefab, out Vector2 position, out float angle)
    {
        float checkRadius = EnemyController.CalculateRadius(prefab.transform) + spawnClearance;
        float spawnRadius = Radius * ringScale + enemySpawnGap;
        ContactFilter2D solidOnly = new() { useTriggers = false };

        for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
        {
            angle = Random.Range(0f, 360f);
            float radians = angle * Mathf.Deg2Rad;
            position = (Vector2)transform.position +
                       new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * spawnRadius;

            Physics2D.OverlapCircle(position, checkRadius, solidOnly, _spotHits);
            if (!HitsAShip(_spotHits))
            {
                return true;
            }
        }

        position = default;
        angle = 0f;
        return false;
    }

    // Only ships and loose tiles block a spawn spot (not background hazards
    // or anything else with a big collider).
    private static bool HitsAShip(List<Collider2D> hits)
    {
        foreach (Collider2D hit in hits)
        {
            if (hit.GetComponentInParent<ShipManager>() || hit.GetComponentInParent<Tile>())
            {
                return true;
            }
        }

        return false;
    }

    // EnemyDefeatedEvent fires for every enemy in the game, so only react
    // to the ones this station sent.
    private void HandleEnemyDefeated(EnemyController enemy, int points)
    {
        if (spawnedEnemies.Remove(enemy))
        {
            RecalculatePressure();
        }
    }

    // Adds up the PressureScore of every living enemy this station sent.
    // Also drops any that were destroyed without announcing it.
    private void RecalculatePressure()
    {
        spawnedEnemies.RemoveAll(enemy => !enemy);
        int total = 0;
        foreach (EnemyController enemy in spawnedEnemies)
        {
            if (enemy.Info)
            {
                total += enemy.Info.PressureScore;
            }
        }

        currentPressure = total;
    }

    // How far the station's furthest tile corner reaches from its centre.
    public float CalculateRadius()
    {
        float radius = 0f;
        Vector2 stationCentre
            = new Vector2(transform.position.x, transform.position.y);

        foreach (SpriteRenderer tile in
                 GetComponentsInChildren<SpriteRenderer>())
        {
            Bounds bounds = tile.bounds;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    Vector2 corner = new Vector2(
                        bounds.center.x + extents.x * x,
                        bounds.center.y + extents.y * y);
                    radius = Mathf.Max(radius,
                        Vector2.Distance(stationCentre, corner));
                }
            }
        }

        return radius;
    }
}
}
