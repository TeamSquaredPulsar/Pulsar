using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Pulsar.Player;
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

    private List<EnemyController> _spawnedEnemies = new();
    private Coroutine _waveCoroutine;

    private void Awake()
    {
        EnemyController.EnemyDefeatedEvent += HandleEnemyDefeated;
    }

    private void OnDestroy()
    {
        if (_waveCoroutine is not null)
        {
            StopCoroutine(_waveCoroutine);
        }
 
        foreach (EnemyController enemy in _spawnedEnemies.Where(enemy =>
                     enemy.gameObject))
        {
            Destroy(enemy.gameObject);
        }
        
        EnemyController.EnemyDefeatedEvent -= HandleEnemyDefeated;
    }

    public void StartWave()
    {
        _waveCoroutine = StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        foreach (WaveEntry entry in _wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyPrefab);
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }

        _waveCoroutine = null;
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        float newRadius
            = EnemyController.CalculateRadius(enemyPrefab.transform);
        Vector3 spawnPosition
            = PlayerController.PlayerPosition + RandomOffset();

        for (int attempt = 0;
             attempt < MaxSpawnAttempts &&
             IsTooCloseToAnEnemy(spawnPosition, newRadius);
             attempt++)
        {
            spawnPosition = PlayerController.PlayerPosition + RandomOffset();
        }

        GameObject go
            = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        if (!go.TryGetComponent(out EnemyController controller))
        {
            throw new ArgumentException(
                "Object in spawner does not have an EnemyController!");
        }

        _spawnedEnemies.Add(controller);
    }

    private void HandleEnemyDefeated(EnemyController killed, int _)
    {
        _spawnedEnemies.Remove(killed);
        if (_waveCoroutine is not null && _spawnedEnemies.Count == 0)
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