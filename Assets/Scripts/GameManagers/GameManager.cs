using System;
using NaughtyAttributes;
using Pulsar.Audio;
using Pulsar.DataClasses;
using Pulsar.Player;
using Pulsar.Stations;
using Pulsar.Stations.Guards;
using Pulsar.UI;
using UnityEngine;

namespace Pulsar
{
public class GameManager : Singleton<GameManager>
{
    public static event Action<int> OnEnemyKill;

    [SerializeField, RequiredType(typeof(PlayerController)),]
    private GameObject playerPrefab;

    // The old wave system (EnemySpawner, WaveDefinition, Wave01-10) is unplugged:
    // enemies now come from the space stations (see SpaceStation). The old
    // classes and assets stay in the project until the stations are proven.

    private int _score;

    [BoxGroup("Audio")]
    [SerializeField]
    private AudioManager audioManager;

    [BoxGroup("Audio")]
    [SerializeField]
    private FmodEvents audioEvents;

    protected override void Awake()
    {
        base.Awake();
        UIManager.GameStateChangeEvent += OnGameStateChange;
        EnemyController.EnemyDefeatedEvent += HandleEnemyKilled;
        EnemyGuardAI.GuardDefeatedEvent += HandleGuardKilled;
    }

    private void HandleEnemyKilled(EnemyController _, int points)
    {
        AddScore(points);
    }

    private void HandleGuardKilled(EnemyGuardAI _, int points)
    {
        AddScore(points);
    }

    private void AddScore(int points)
    {
        _score += points;
        OnEnemyKill?.Invoke(_score);
    }

    private void OnDestroy()
    {
        UIManager.GameStateChangeEvent -= OnGameStateChange;
        EnemyController.EnemyDefeatedEvent -= HandleEnemyKilled;
        EnemyGuardAI.GuardDefeatedEvent -= HandleGuardKilled;
    }

    private void OnGameStateChange(GameState obj)
    {
        switch (obj)
        {
            case GameState.MainMenu:
                ResetStations();
                break;
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

    private void HandleGameOver()
    {
        ResetStations();
    }

    private void HandleGameStart()
    {
        _score = 0;
        Tile[] tiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (Tile tile in tiles)
        {
            Destroy(tile.gameObject);
        }

        Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        // Every station in the scene starts guarding from its first wave.
        foreach (SpaceStation station in FindStations())
        {
            station.BeginDefense();
        }
    }

    private static void ResetStations()
    {
        foreach (SpaceStation station in FindStations())
        {
            station.ResetStation();
        }
    }

    private static SpaceStation[] FindStations() =>
        FindObjectsByType<SpaceStation>(FindObjectsSortMode.None);
}
}
