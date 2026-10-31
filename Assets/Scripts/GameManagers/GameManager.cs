using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.Player;
using UnityEngine;

namespace Pulsar
{
public class GameManager : MonoBehaviour
{
    public static event Action<int> OnEnemyKill;
    
    [SerializeField, RequiredType(typeof(PlayerController)),]
    private GameObject playerPrefab;

    [SerializeField, RequiredType(typeof(EnemySpawner)),]
    private GameObject enemySpawnerPrefab;

    [SerializeField]
    private int waveDelaySeconds;
    
    [SerializeField, ReorderableList,]
    private List<WaveDefinitionSO> waveDefinitions;
    
    private EnemySpawner _enemySpawner;
    private int _currentWave;
    private int _score;
    private Coroutine _awaitingWave;
    
    private void Awake()
    {
        UIManager.GameStateChangeEvent += OnGameStateChange;
        EnemyController.EnemyDefeatedEvent += HandleEnemyKilled;
    }

    private void HandleEnemyKilled(EnemyController _, int points)
    {
        _score += points;
        OnEnemyKill?.Invoke(_score);
    }

    private void OnDestroy()
    {
        UIManager.GameStateChangeEvent -= OnGameStateChange;
    }

    private void OnGameStateChange(GameState obj)
    {
        switch (obj)
        {
            case GameState.MainMenu:
                _currentWave = 0;
                break;
            case GameState.Playing:
                Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                _enemySpawner = Instantiate(enemySpawnerPrefab, Vector3.zero,
                    Quaternion.identity).GetComponent<EnemySpawner>();
                _enemySpawner.WaveCompleteEvent += HandleWaveComplete;
                LoadNextWave();
                break;
            case GameState.GameOver:
                if (_awaitingWave is not null)
                {
                    StopCoroutine(_awaitingWave);
                }
                Destroy(_enemySpawner);
                _currentWave = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }
    }

    private void HandleWaveComplete()
    {
        _currentWave++;
        if (_currentWave >= waveDefinitions.Count)
        {
            //TODO: Victory screen.
            return;
        }
        LoadNextWave();
    }

    private void LoadNextWave()
    {
        _enemySpawner.LoadNewWave(waveDefinitions[_currentWave]);
        _awaitingWave = StartCoroutine(StartNextWaveAfterDelay());
    }

    private IEnumerator StartNextWaveAfterDelay()
    {
        yield return new WaitForSeconds(waveDelaySeconds);
        _enemySpawner.StartWave();
        _awaitingWave = null;
    }
}
}

