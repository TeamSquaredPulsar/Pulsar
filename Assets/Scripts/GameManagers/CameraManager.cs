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
        PlayerController.PlayerSpawnedEvent += OnPlayerSpawned;
        PlayerController.PlayerLossEvent += OnPlayerLoss;
        mainMenuCamera.Prioritize();
    }

    private void Start()
    {
        mainMenuCamera.Prioritize();
    }

    private void OnDestroy()
    {
        PlayerController.PlayerSpawnedEvent -= OnPlayerSpawned;
        PlayerController.PlayerLossEvent -= OnPlayerLoss;
    }
    
    private void OnPlayerLoss(PlayerController obj)
    {
        targetGroup.RemoveMember(obj.transform);
        mainMenuCamera.transform.position = gameplayCamera.transform.position;
        mainMenuCamera.Prioritize();
    }

    private void OnPlayerSpawned(PlayerController obj)
    {
        targetGroup.AddMember(obj.transform, playerWeight, playerRadius);
        gameplayCamera.Prioritize();
    }
}
}