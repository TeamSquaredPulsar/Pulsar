using NaughtyAttributes;
using Pulsar.DataClasses;
using Pulsar.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Pulsar.UI
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

    [BoxGroup("Target Group"), SerializeField,]
    private CinemachineTargetGroup targetGroup;

    [BoxGroup("Target Group"), SerializeField,]
    private float playerRadius;

    [BoxGroup("Target Group")]
    [SerializeField]
    private float playerWeight;

    private const int k_HighPriority = 100;
    private const int k_LowPriority = 0;

    private void Awake()
    {
        cameraDataAsset.MainCamera = mainCamera;
        PlayerController.PlayerSpawnedEvent += OnPlayerSpawned;
        PlayerController.PlayerLossEvent += OnPlayerLoss;
    }

    private void Start()
    {
        PrioritizeMenuCamera();
    }

    private void OnDestroy()
    {
        PlayerController.PlayerSpawnedEvent -= OnPlayerSpawned;
        PlayerController.PlayerLossEvent -= OnPlayerLoss;
    }

    private void PrioritizeMenuCamera()
    {
        mainMenuCamera.Priority = k_HighPriority;
        gameplayCamera.Priority = k_LowPriority;
    }

    private void PrioritizeGameplayCamera()
    {
        mainMenuCamera.Priority = k_LowPriority;
        gameplayCamera.Priority = k_HighPriority;
    }

    private void OnPlayerLoss(PlayerController obj)
    {
        targetGroup.RemoveMember(obj.transform);
        mainMenuCamera.transform.position = gameplayCamera.transform.position;
        PrioritizeMenuCamera();
    }

    private void OnPlayerSpawned(PlayerController obj)
    {
        targetGroup.AddMember(obj.transform, playerWeight, playerRadius);
        PrioritizeGameplayCamera();
    }
}
}