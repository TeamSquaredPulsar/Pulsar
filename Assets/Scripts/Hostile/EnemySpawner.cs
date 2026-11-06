using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.Player;
using UnityEngine;
using Random = UnityEngine.Random;

// Spawns one wave at a time for the GameManager. GameManager decides which
// wave comes next; the spawner decides where and how its enemies appear,
// and says when the wave is cleared.
public class EnemySpawner : MonoBehaviour
{
    private const int MaxSpawnAttempts = 30;
    private const float FullCircle = Mathf.PI * 2f;
    private const float LeashCheckInterval = 1f;

    public event Action WaveCompleteEvent;

    [ShowNonSerializedField]
    private WaveDefinitionSO _wave;

    [Header("Spawning")]
    [Tooltip("Distance from the player to the enemy's edge, not its center.")]
    [SerializeField]
    private float spawnRadiusMin = 10f;

    [SerializeField]
    private float spawnRadiusMax = 14f;

    [SerializeField]
    private float spawnPadding = 1f;

    [Header("Strays")]
    [Tooltip("Enemies further than this from the player are brought back " +
             "to a spawn point. 0 = off.")]
    [SerializeField]
    [Min(0f)]
    private float leashDistance = 40f;

    private readonly List<EnemyController> _spawnedEnemies = new();
    private Coroutine _waveCoroutine;
    private float _patternAngle;
    private int _spawnCount;

    private void Awake()
    {
        EnemyController.EnemyDefeatedEvent += HandleEnemyDefeated;
    }

    private void Start()
    {
        StartCoroutine(LeashStrays());
    }

    private void OnDestroy()
    {
        if (_waveCoroutine is not null)
        {
            StopCoroutine(_waveCoroutine);
        }

        foreach (EnemyController enemy in _spawnedEnemies)
        {
            if (enemy)
            {
                if (enemy)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }

        EnemyController.EnemyDefeatedEvent -= HandleEnemyDefeated;
    }

    public void LoadNewWave(WaveDefinitionSO newWave)
    {
        _wave = newWave;
    }

    public void StartWave()
    {
        if (!_wave)
        {
            Debug.LogError("StartWave called before LoadNewWave.");
            return;
        }

        if (_waveCoroutine is not null)
        {
            StopCoroutine(_waveCoroutine);
        }

        _waveCoroutine = StartCoroutine(RunWave(_wave));
    }

    // Spawns the whole wave, then waits until every enemy from it is gone
    // (or the wave's safety timer runs out) and reports the wave complete
    // exactly once.
    private IEnumerator RunWave(WaveDefinitionSO wave)
    {
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

        float timeoutAt = Time.time + wave.maxWaveTime;
        yield return new WaitUntil(() =>
        {
            // Drop enemies destroyed some other way, so they can't hold
            // the wave open forever.
            _spawnedEnemies.RemoveAll(enemy => !enemy);
            return _spawnedEnemies.Count == 0 ||
                   (wave.maxWaveTime > 0f && Time.time >= timeoutAt);
        });

        if (_spawnedEnemies.Count > 0)
        {
            Debug.LogWarning(
                $"Wave {wave.waveName} timed out with " +
                $"{_spawnedEnemies.Count} enemies left; moving on anyway.");
        }

        _waveCoroutine = null;
        WaveCompleteEvent?.Invoke();
    }

    private void HandleEnemyDefeated(EnemyController killed, int _)
    {
        _spawnedEnemies.Remove(killed);
    }

    // Once a second, any enemy that has wandered too far from the player
    // (flung off by a hit, stuck spinning, etc.) is put back at a normal
    // spawn point, so every wave can always be finished.
    private IEnumerator LeashStrays()
    {
        WaitForSeconds wait = new(LeashCheckInterval);
        while (true)
        {
            yield return wait;
            if (leashDistance <= 0f)
            {
                continue;
            }

            float sqrLeash = leashDistance * leashDistance;
            foreach (EnemyController enemy in EnemyController.ActiveEnemies)
            {
                Vector3 offset = enemy.transform.position -
                                 PlayerController.PlayerPosition;
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
        GameObject go = Instantiate(enemyPrefab, FindSpawnPosition(newRadius),
            Quaternion.identity);
        _spawnCount++;

        if (!go.TryGetComponent(out EnemyController controller))
        {
            throw new ArgumentException(
                "Object in spawner does not have an EnemyController!");
        }

        _spawnedEnemies.Add(controller);
    }

    // A spot around the player that follows the wave's pattern and isn't
    // on top of another enemy (gives up after MaxSpawnAttempts tries).
    private Vector3 FindSpawnPosition(float radius)
    {
        Vector3 position = PlayerController.PlayerPosition + SpawnOffset(radius);
        for (int attempt = 0;
             attempt < MaxSpawnAttempts &&
             IsTooCloseToAnEnemy(position, radius);
             attempt++)
        {
            position = PlayerController.PlayerPosition + SpawnOffset(radius);
        }

        return position;
    }

    private Vector3 SpawnOffset(float enemyRadius)
    {
        SpawnPattern pattern = _wave ? _wave.pattern : SpawnPattern.Surround;
        float spread = _wave ? _wave.patternSpread : 0f;
        float halfSpread = spread * 0.5f * Mathf.Deg2Rad;
        float angle = pattern switch
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
