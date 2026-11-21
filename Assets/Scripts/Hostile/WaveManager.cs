using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.DataClasses;
using UnityEngine;

namespace Pulsar.Hostile
{
public class WaveManager : MonoBehaviour
{
    [SerializeField]
    private EnemySpawner enemySpawner;

    [SerializeField]
    private int waveDelaySeconds;

    [SerializeField]
    [ReorderableList]
    private List<WaveDefinition> waveDefinitions;

    private int _currentWave;

    private Coroutine _awaitingWave;

    private void OnEnable()
    {
        StateManager.GameStateChanged += OnGameStateChange;
    }

    private void OnDisable()
    {
        StateManager.GameStateChanged -= OnGameStateChange;
    }

    private void OnGameStateChange(GameState obj)
    {
        switch (obj)
        {
            case GameState.MainMenu:
            case GameState.Loading:
            case GameState.Sandbox:
            case GameState.Tutorial:
            case GameState.Playing:
                HandleGameStart();
                break;
            case GameState.GameOver:
                HandleGameOver();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }
    }
    private void HandleGameStart()
    {
        enemySpawner.WaveCompleteEvent += HandleWaveComplete;
        LoadNextWave();
    }

    private void HandleGameOver()
    {
        if (_awaitingWave is not null)
        {
            StopCoroutine(_awaitingWave);
        }

        Destroy(enemySpawner.gameObject);
        _currentWave = 0;
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
        if (_currentWave >= waveDefinitions.Count)
        {
            Debug.Log("Cleared all waves");
            return;
        }
        enemySpawner.LoadNewWave(waveDefinitions[_currentWave]);
        _awaitingWave = StartCoroutine(StartNextWaveAfterDelay());
    }

    private IEnumerator StartNextWaveAfterDelay()
    {
        yield return new WaitForSeconds(waveDelaySeconds);
        enemySpawner.StartWave();
        _awaitingWave = null;
    }
}
}
