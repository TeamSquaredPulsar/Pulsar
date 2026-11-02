using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    private const int MaxSpawnAttempts = 30;
    private const float FullCircle = Mathf.PI * 2f;
    private const float LeashCheckInterval = 1f;

    [Header("Waves")]
    [SerializeField]
    private List<WaveDefinitionSO> waves = new();

    [Tooltip("After the last wave, play again from this wave (1 = first).")]
    [SerializeField]
    [Min(1)]
    private int loopFromWave = 1;

    [Tooltip("Breather between clearing a wave and the next one starting.")]
    [SerializeField]
    [Min(0f)]
    private float timeBetweenWaves = 4f;

    [Tooltip("For testing: start at this wave instead of the first (1 = first).")]
    [SerializeField]
    [Min(1)]
    private int startAtWave = 1;

    [Header("Spawning")]
    [Tooltip("Distance from the player to the enemy's edge, not its center.")]
    [SerializeField]
    private float spawnRadiusMin = 10f;

    [SerializeField]
    private float spawnRadiusMax = 14f;

    [SerializeField]
    private float spawnPadding = 1f;

    [Tooltip("How wide (in degrees) the OneSide and Pincer patterns spread.")]
    [SerializeField]
    [Range(0f, 180f)]
    private float patternSpread = 40f;

    [Header("Strays")]
    [Tooltip("Enemies further than this from the player are brought back " +
             "to a spawn point. 0 = off.")]
    [SerializeField]
    [Min(0f)]
    private float leashDistance = 40f;

    [Tooltip("Safety net: seconds after a wave finishes spawning before the " +
             "next wave starts even if enemies are left. 0 = off.")]
    [SerializeField]
    [Min(0f)]
    private float maxWaveTime = 120f;

    private Transform _player;
    private SpawnPattern _pattern;
    private float _patternAngle;
    private int _spawnCount;

    // 1-based number of the wave being played, 0 before the first one.
    public int CurrentWaveNumber { get; private set; }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            throw new InvalidOperationException("Player not found");
        }

        _player = playerObj.transform;

        if (waves.Count > 0)
        {
            StartCoroutine(RunWaves());
            StartCoroutine(LeashStrays());
        }
    }

    // Plays the waves in order. The next wave only starts once every enemy
    // from the current one is gone, plus a short breather, so waves never
    // pile up on the player. After the last wave it loops back.
    private IEnumerator RunWaves()
    {
        int index = Mathf.Clamp(startAtWave - 1, 0, waves.Count - 1);
        int loopIndex = Mathf.Clamp(loopFromWave - 1, 0, waves.Count - 1);

        while (true)
        {
            CurrentWaveNumber = index + 1;
            WaveDefinitionSO wave = waves[index];
            Debug.Log($"Wave {CurrentWaveNumber}: {wave.waveName}");

            yield return SpawnWave(wave);

            float timeoutAt = Time.time + maxWaveTime;
            yield return new WaitUntil(
                () => EnemyController.ActiveEnemies.Count == 0 ||
                      (maxWaveTime > 0f && Time.time >= timeoutAt));

            if (EnemyController.ActiveEnemies.Count > 0)
            {
                Debug.LogWarning(
                    $"Wave {CurrentWaveNumber} timed out with " +
                    $"{EnemyController.ActiveEnemies.Count} enemies left; " +
                    "starting the next wave anyway.");
            }

            yield return new WaitForSeconds(timeBetweenWaves);

            index++;
            if (index >= waves.Count)
            {
                index = loopIndex;
            }
        }
    }

    private IEnumerator SpawnWave(WaveDefinitionSO wave)
    {
        _pattern = wave.pattern;
        _patternAngle = Random.Range(0f, FullCircle);
        _spawnCount = 0;

        foreach (WaveEntry entry in wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyPrefab);
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }
    }

    // Once a second, any enemy that has wandered too far from the player
    // (flung off by a hit, stuck spinning, etc.) is put back at a normal
    // spawn point, so every wave can always be finished.
    private IEnumerator LeashStrays()
    {
        WaitForSeconds wait = new(LeashCheckInterval);
        while (_player != null)
        {
            yield return wait;
            if (leashDistance <= 0f || _player == null)
            {
                continue;
            }

            float sqrLeash = leashDistance * leashDistance;
            foreach (EnemyController enemy in EnemyController.ActiveEnemies)
            {
                Vector3 offset = enemy.transform.position - _player.position;
                offset.z = 0f;
                if (offset.sqrMagnitude > sqrLeash)
                {
                    enemy.Recall(FindSpawnPosition(enemy.Radius));
                }
            }
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        float newRadius
            = EnemyController.CalculateRadius(enemyPrefab.transform);
        Instantiate(enemyPrefab, FindSpawnPosition(newRadius),
            Quaternion.identity);
        _spawnCount++;
    }

    // A spot around the player that follows the wave's pattern and isn't
    // on top of another enemy (gives up after MaxSpawnAttempts tries).
    private Vector3 FindSpawnPosition(float radius)
    {
        Vector3 position = _player.position + SpawnOffset(radius);
        for (int attempt = 0;
             attempt < MaxSpawnAttempts &&
             IsTooCloseToAnEnemy(position, radius);
             attempt++)
        {
            position = _player.position + SpawnOffset(radius);
        }

        return position;
    }

    private Vector3 SpawnOffset(float enemyRadius)
    {
        float halfSpread = patternSpread * 0.5f * Mathf.Deg2Rad;
        float angle = _pattern switch
        {
            SpawnPattern.OneSide => _patternAngle +
                                    Random.Range(-halfSpread, halfSpread),
            // Even spawns on one side, odd spawns on the opposite side.
            SpawnPattern.Pincer => _patternAngle +
                                   (_spawnCount % 2) * Mathf.PI +
                                   Random.Range(-halfSpread, halfSpread),
            _ => Random.Range(0f, FullCircle),
        };

        // Measured to the enemy's edge, so big ships don't spawn on top of
        // the player.
        float radius = Random.Range(spawnRadiusMin, spawnRadiusMax) +
                       enemyRadius;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }

    private bool IsTooCloseToAnEnemy(Vector3 position, float radius)
    {
        foreach (EnemyController enemy in EnemyController.ActiveEnemies)
        {
            if (Vector3.Distance(position, enemy.transform.position) <
                radius + enemy.Radius + spawnPadding)
            {
                return true;
            }
        }

        return false;
    }
}