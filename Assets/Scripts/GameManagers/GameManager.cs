using System;
using System.Threading.Tasks;
using Audune.Utils.Dictionary;
using NaughtyAttributes;
using Pulsar.Audio;
using Pulsar.DataClasses;
using Pulsar.Player;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Pulsar
{
public class GameManager : Singleton<GameManager>
{
    public static event Action<int> OnScoreChange;

    [BoxGroup("References")]
    [SerializeField]
    [RequiredType(typeof(PlayerController))]
    private GameObject playerPrefab;

    [SerializeField]
    [field:
        SerializableDictionaryOptions(keyHeader = "State",
            valueHeader = "Scene")]
    private SerializableDictionary<GameState, SceneReference> scenes = new();

    [BoxGroup("Audio")]
    [SerializeField]
    private AudioManager audioManager;

    [BoxGroup("Audio")]
    [SerializeField]
    private FmodEvents audioEvents;

    private int Score
    {
        get => _score;
        set
        {
            _score = value;
            OnScoreChange?.Invoke(_score);
        }
    }

    private int _score;

    protected override void Awake()
    {
        base.Awake();
        EnemyController.EnemyDefeatedEvent += HandleEnemyKilled;
        StateManager.GameStateChanged += OnGameStateChange;
    }

    private void OnDestroy()
    {
        StateManager.GameStateChanged -= OnGameStateChange;
        EnemyController.EnemyDefeatedEvent -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled(EnemyController _, int points)
    {
        Score += points;
    }

    private void OnGameStateChange(GameState obj)
    {
        //Handle case-specific logic here.
        switch (obj)
        {
            case GameState.Sandbox:
            case GameState.Tutorial:
            case GameState.Playing:
                HandleGameStart();
                break;
            case GameState.GameOver:
            case GameState.MainMenu:
            case GameState.Loading:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }
    }

    private void HandleGameStart()
    {
        Score = 0;
        Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
    }

    public async Task LoadLevel(GameState levelState)
    {
        StateManager.SetCurrentState(GameState.Loading);

        CleanupTiles();

        if (!scenes.TryGetValue(levelState, out SceneReference scene))
        {
            throw new InvalidOperationException(
                $"No scene found for state {levelState}!");
        }

        AsyncOperationHandle<SceneInstance> sceneLoading
            = Addressables.LoadSceneAsync(scene);
        await sceneLoading.Task;
        if (sceneLoading.Status != AsyncOperationStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to load {scene.SceneName}!");
        }

        StateManager.SetCurrentState(levelState);
    }

    private static void CleanupTiles()
    {
        Tile[] tiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (Tile tile in tiles)
        {
            Destroy(tile.gameObject);
        }
    }
}
}
