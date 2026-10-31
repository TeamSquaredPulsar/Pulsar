using System;
using NaughtyAttributes;
using Pulsar.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Pulsar
{
public class CameraManager : MonoBehaviour
{
    [SerializeField]
    private CameraData cameraDataAsset;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private CinemachineCamera mainMenuCamera;
    [SerializeField]
    private CinemachineCamera gameplayCamera;

    [BoxGroup("Target Group"),
     RequiredType(typeof(PlayerController)),
     SerializeField,]
    private GameObject pPlayerPrefab;

    [BoxGroup("Target Group"), SerializeField,]
    private CinemachineTargetGroup targetGroup;

    [BoxGroup("Target Group"), SerializeField,]
    private float playerRadius;

    [BoxGroup("Target Group"), SerializeField,]
    private float playerWeight;

    private void Awake()
    {
        cameraDataAsset.MainCamera = mainCamera;
        UIManager.GameStateChangeEvent += OnGameStateChange;
        PlayerController.PlayerSpawnedEvent += OnPlayerSpawned;
        PlayerController.PlayerLossEvent += OnPlayerLoss;
        mainMenuCamera.Prioritize();
    }

    private void OnPlayerLoss(PlayerController obj)
    {
        targetGroup.RemoveMember(obj.transform);
    }

    private void OnPlayerSpawned(PlayerController obj)
    {
        targetGroup.AddMember(obj.transform, playerWeight, playerRadius);
    }

    private void OnDestroy()
    {
        UIManager.GameStateChangeEvent -= OnGameStateChange;
        PlayerController.PlayerSpawnedEvent -= OnPlayerSpawned;
        PlayerController.PlayerLossEvent -= OnPlayerLoss;
    }

    private void OnGameStateChange(GameState obj)
    {
        switch (obj)
        {
            case GameState.MainMenu:
                mainMenuCamera.Prioritize();
                break;
            case GameState.Playing:
                gameplayCamera.Prioritize();
                break;
            case GameState.GameOver:
                mainMenuCamera.Prioritize();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }

    }
}
}