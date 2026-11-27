using System;
using System.Collections;
using System.Collections.Generic;
using Pulsar.Stations.Generation;
using Pulsar.Stations.Guards;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Stations
{
/// <summary>
/// One script for every station. The station's StationProfile says what's
/// different (colour tiles, weapons, waves). When the station is enabled (its
/// scene loads), it builds its first wave of guards with the EnemyGenerator and spreads them on
/// a ring around itself. A new, harder wave comes once the last one is
/// destroyed, up to the profile's wave count. Guards only patrol around the
/// station; they never chase the player. It doesn't follow the game's state: OnEnable starts it from wave 1, and
/// OnDisable (which runs when its scene unloads) clears its guards. So it works
/// the same in test scenes and in the real game.
///
/// It also sends enemies at the player, topping them up under the profile's
/// MaxPressure: each living enemy counts its PressureScore, and the station
/// only spawns one whose score fits the room left. The list of living enemies
/// is the truth; the current pressure is always added up from it.
/// </summary>
public class SpaceStation : MonoBehaviour
{
    [SerializeField]
    private StationProfile profile;

    [Tooltip(
        "How far out the guards spawn and patrol, as a multiple of the station's own radius.")]
    [SerializeField]
    private float ringScale = 1.8f;

    [Header("Debug (read only)")]
    [SerializeField]
    private int currentWave = -1;

    [SerializeField]
    private int currentPressure;

    // The guards this station spawned that are still alive.
    private readonly List<EnemyGuardAI> spawnedGuards = new();

    // The enemies this station sent that are still alive.
    private readonly List<EnemyController> spawnedEnemies = new();

    private bool _defending;
    private Coroutine _nextWaveRoutine;
    private Coroutine _spawnRoutine;

    public StationProfile Profile => profile;

    // How far the station's furthest tile reaches (measured when it starts defending).
    public float Radius { get; private set; }
    public bool IsCleared { get; private set; }
    public int CurrentPressure => currentPressure;
    public bool IsSpawning => _spawnRoutine != null;

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

    // Every wave of this station was destroyed.
    public static event Action<SpaceStation> StationClearedEvent;

    // Starts the station over from wave 1.
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
        _defending = true;
        SpawnWave(0);
        // TODO: WaveManager will call StartSpawning / StopSpawning once it
        // drives the stations. Until then the station starts on its own.
        StartSpawning();
    }

    // Removes this station's guards and stops it (OnDisable, and before a restart).
    public void ResetStation()
    {
        if (_nextWaveRoutine != null)
        {
            StopCoroutine(_nextWaveRoutine);
            _nextWaveRoutine = null;
        }

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
        currentWave = -1;
        _defending = false;
        IsCleared = false;
    }

    private void SpawnWave(int waveIndex)
    {
        currentWave = waveIndex;
        int guardCount = profile.Waves[waveIndex].GuardCount;
        Vector2 centre = transform.position;
        float ringRadius = Radius * ringScale;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < guardCount; i++)
        {
            float angle = startAngle + i * 360f / guardCount;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 position = centre + new Vector2(Mathf.Cos(radians),
                Mathf.Sin(radians)) * ringRadius;
            // Nose pointing along the ring, the way it will patrol.
            float facing = angle;

            EnemyGuardAI guard = EnemyGenerator.GenerateEnemy(profile,
                waveIndex, position, facing);
            if (!guard)
            {
                continue;
            }

            guard.Init(this, profile.GuardInfo, ringRadius, angle);
            guard.Defeated += HandleGuardDefeated;
            spawnedGuards.Add(guard);
        }
    }

    private void HandleGuardDefeated(EnemyGuardAI guard)
    {
        guard.Defeated -= HandleGuardDefeated;
        spawnedGuards.Remove(guard);
        if (spawnedGuards.Count > 0 || !_defending)
        {
            return;
        }

        int nextWave = currentWave + 1;
        if (nextWave < profile.Waves.Length)
        {
            _nextWaveRoutine = StartCoroutine(SpawnWaveAfterDelay(nextWave));
            return;
        }

        _defending = false;
        IsCleared = true;
        Debug.Log($"{name} cleared!");
        StationClearedEvent?.Invoke(this);
    }

    private IEnumerator SpawnWaveAfterDelay(int waveIndex)
    {
        yield return new WaitForSeconds(profile.SecondsBetweenWaves);
        _nextWaveRoutine = null;
        SpawnWave(waveIndex);
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

        // Comes out on the guard ring at a random angle, nose pointing away
        // from the station. (The bay comes later.)
        float angle = Random.Range(0f, 360f);
        float radians = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        Vector2 position = (Vector2)transform.position
                           + direction * (Radius * ringScale);
        Quaternion facing = Quaternion.Euler(0f, 0f, angle - 90f);

        EnemyController enemy = Instantiate(chosen, position, facing);
        spawnedEnemies.Add(enemy);
        RecalculatePressure();
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
