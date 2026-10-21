using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private WaveDefinitionSO wave;

    [SerializeField]
    private float spawnRadiusMin = 10f;

    [SerializeField]
    private float spawnRadiusMax = 14f;

    [SerializeField]
    private float minSpawnDistance = 3f;

    private const int MaxSpawnAttempts = 30;

    private Transform _player;

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            throw new InvalidOperationException("Player not found");
        }

        _player = playerObj.transform;

        StartWave();
    }

    public void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        foreach (WaveEntry entry in wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyPrefab);
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        Vector3 spawnPosition = _player.position + RandomOffset();

        for (int attempt = 0; attempt < MaxSpawnAttempts && IsTooCloseToAnEnemy(spawnPosition); attempt++)
        {
            spawnPosition = _player.position + RandomOffset();
        }

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }

    private Vector3 RandomOffset()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(spawnRadiusMin, spawnRadiusMax);
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }

    private bool IsTooCloseToAnEnemy(Vector3 position)
    {
        foreach (EnemyController enemy in EnemyController.ActiveEnemies)
        {
            if (Vector3.Distance(position, enemy.transform.position) < minSpawnDistance)
            {
                return true;
            }
        }

        return false;
    }
}
