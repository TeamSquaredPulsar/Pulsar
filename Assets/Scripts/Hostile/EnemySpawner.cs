using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    private const int MaxSpawnAttempts = 30;
    
    public event Action WaveCompleteEvent;

    [ShowNonSerializedField]
    private WaveDefinitionSO _wave;

    [SerializeField]
    private float spawnRadiusMin = 10f;

    [SerializeField]
    private float spawnRadiusMax = 14f;

    [SerializeField]
    private float spawnPadding = 1f;

    private Transform _player;

    private int _spawnedEnemies;
    private bool _isSpawning = false;

    private void Awake()
    {
        EnemyController.EnemyDefeatedEvent += HandleEnemyDefeated;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            throw new InvalidOperationException("Player not found");
        }
        
        foreach (EnemyController enemy in EnemyController.ActiveEnemies)
        {
            Destroy(enemy.gameObject);
        }

        _player = playerObj.transform;
    }

    private void OnDestroy()
    {
        EnemyController.EnemyDefeatedEvent -= HandleEnemyDefeated;
    }

    public void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        _isSpawning = true;
        foreach (WaveEntry entry in _wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyPrefab);
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }
        _isSpawning = false;
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        float newRadius
            = EnemyController.CalculateRadius(enemyPrefab.transform);
        Vector3 spawnPosition = _player.position + RandomOffset();

        for (int attempt = 0;
             attempt < MaxSpawnAttempts &&
             IsTooCloseToAnEnemy(spawnPosition, newRadius);
             attempt++)
        {
            spawnPosition = _player.position + RandomOffset();
        }

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        _spawnedEnemies++;
    }

    private void HandleEnemyDefeated(int _)
    {
        _spawnedEnemies--;
        if (!_isSpawning && _spawnedEnemies == 0)
        {
            WaveCompleteEvent?.Invoke();
        }
    }

    public void LoadNewWave(WaveDefinitionSO newWave)
    {
        _wave = newWave;
    }

    private Vector3 RandomOffset()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(spawnRadiusMin, spawnRadiusMax);
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