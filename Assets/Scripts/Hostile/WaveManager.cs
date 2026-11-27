using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.DataClasses;
using UnityEngine;

namespace Pulsar.Hostile
{
// One spot where a wave puts a station.
[Serializable]
public class StationSpawn
{
    [Tooltip("A station prefab with a SpaceStation (anything that is an IWaveEnemySource).")]
    public GameObject stationPrefab;

    [Tooltip("Where it appears. Place an empty object in the scene for each spot.")]
    public Transform spawnPoint;
}

// One wave: the stations it brings and how long they send enemies.
[Serializable]
public class StationWave
{
    public string waveName;

    [Tooltip("Seconds the stations send enemies. After that they stop, and the " +
             "wave ends once every enemy they sent is dead.")]
    [Min(1f)]
    public float durationSeconds = 60f;

    public List<StationSpawn> stations = new();
}

/// <summary>
/// Runs the station waves. Each wave puts its stations in the world, switches
/// them on for durationSeconds, then switches them off and waits until every
/// enemy they sent is dead. After waveDelaySeconds the next wave starts.
/// Stations from earlier waves stay in the world but never send enemies
/// again (dormant): their guards keep circling.
/// WaveManager only talks to IWaveEnemySource, so it doesn't depend on the
/// station code.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [SerializeField]
    private int waveDelaySeconds;

    [SerializeField]
    [ReorderableList]
    private List<StationWave> stationWaves;

    [Tooltip("Test scenes only: start the waves on Play without waiting for " +
             "the game state (the real game starts them when GameScene loads).")]
    [SerializeField]
    private bool startWithoutGameState;

    [Tooltip("Testing overlay: shows the wave phase and its countdown at the " +
             "top of the Game view.")]
    [SerializeField]
    private bool showWaveTimer = true;

    private int _currentWave;

    // What the wave timer overlay shows.
    private enum WavePhase { NotStarted, Waiting, Spawning, Clearing, AllCleared }
    private WavePhase _phase = WavePhase.NotStarted;
    private float _phaseEndsAt;
    private string _currentWaveName;
    private GUIStyle _timerStyle;

    private Coroutine _awaitingWave;
    private Coroutine _runningWave;

    // The current wave's stations, while it runs.
    private readonly List<IWaveEnemySource> _activeSources = new();

    private bool _started;

    private void OnEnable()
    {
        StateManager.GameStateChanged += OnGameStateChange;
    }

    private void Start()
    {
        if (startWithoutGameState)
        {
            HandleGameStart();
        }
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
        // Several states lead here; only the first one starts the waves.
        if (_started)
        {
            return;
        }

        _started = true;
        LoadNextWave();
    }

    private void HandleGameOver()
    {
        if (_awaitingWave is not null)
        {
            StopCoroutine(_awaitingWave);
        }

        if (_runningWave is not null)
        {
            StopCoroutine(_runningWave);
        }

        foreach (IWaveEnemySource source in _activeSources)
        {
            source.StopSpawning();
        }

        _activeSources.Clear();
        _currentWave = 0;
        _started = false;
        _phase = WavePhase.NotStarted;
    }

    private void HandleWaveComplete()
    {
        _currentWave++;
        if (_currentWave >= stationWaves.Count)
        {
            //TODO: Victory screen.
            Debug.Log("Cleared all waves");
            _phase = WavePhase.AllCleared;
            return;
        }

        LoadNextWave();
    }

    private void LoadNextWave()
    {
        if (_currentWave >= stationWaves.Count)
        {
            Debug.Log("Cleared all waves");
            _phase = WavePhase.AllCleared;
            return;
        }

        _awaitingWave = StartCoroutine(StartNextWaveAfterDelay());
    }

    private IEnumerator StartNextWaveAfterDelay()
    {
        _phase = WavePhase.Waiting;
        _phaseEndsAt = Time.time + waveDelaySeconds;
        yield return new WaitForSeconds(waveDelaySeconds);
        _awaitingWave = null;
        _runningWave = StartCoroutine(RunWave(stationWaves[_currentWave]));
    }

    // Spawn the stations, let them send enemies for the wave's duration,
    // switch them off, then wait until every enemy they sent is dead.
    private IEnumerator RunWave(StationWave wave)
    {
        Debug.Log($"Wave {_currentWave + 1} ({wave.waveName}) starts");
        _activeSources.Clear();

        foreach (StationSpawn spawn in wave.stations)
        {
            if (!spawn.stationPrefab || !spawn.spawnPoint)
            {
                Debug.LogWarning(
                    $"Wave {wave.waveName}: a station slot is missing its prefab or spawn point.");
                continue;
            }

            GameObject station = Instantiate(spawn.stationPrefab,
                spawn.spawnPoint.position, spawn.spawnPoint.rotation);

            if (station.TryGetComponent(out IWaveEnemySource source))
            {
                source.StartSpawning();
                _activeSources.Add(source);
            }
            else
            {
                Debug.LogWarning(
                    $"{station.name} has no SpaceStation (IWaveEnemySource), so it sends no enemies.");
            }
        }

        _currentWaveName = wave.waveName;
        _phase = WavePhase.Spawning;
        _phaseEndsAt = Time.time + wave.durationSeconds;
        yield return new WaitForSeconds(wave.durationSeconds);

        foreach (IWaveEnemySource source in _activeSources)
        {
            source.StopSpawning();
        }

        _phase = WavePhase.Clearing;

        yield return new WaitUntil(() =>
        {
            foreach (IWaveEnemySource source in _activeSources)
            {
                if (source.AliveEnemies > 0)
                {
                    return false;
                }
            }

            return true;
        });

        Debug.Log($"Wave {_currentWave + 1} ({wave.waveName}) complete");
        _activeSources.Clear();
        _runningWave = null;
        HandleWaveComplete();
    }

    private int CountAliveEnemies()
    {
        int alive = 0;
        foreach (IWaveEnemySource source in _activeSources)
        {
            alive += source.AliveEnemies;
        }

        return alive;
    }

    // Testing overlay: the wave phase and its countdown, top centre of the Game view.
    private void OnGUI()
    {
        if (!showWaveTimer || _phase == WavePhase.NotStarted)
        {
            return;
        }

        int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(_phaseEndsAt - Time.time));
        int waveNumber = _currentWave + 1;

        string text = _phase switch
        {
            WavePhase.Waiting => $"Next wave in {secondsLeft}s",
            WavePhase.Spawning => $"Wave {waveNumber} ({_currentWaveName}): {secondsLeft}s left",
            WavePhase.Clearing => $"Wave {waveNumber}: kill the rest ({CountAliveEnemies()} enemies left)",
            WavePhase.AllCleared => "All waves cleared",
            _ => string.Empty
        };

        _timerStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(0, 20, Screen.width, 50), text, _timerStyle);
    }
}
}
