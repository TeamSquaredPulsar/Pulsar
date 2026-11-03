using Pulsar.Player;
using UnityEngine;
using Unity.Cinemachine;

namespace Pulsar.UI
{
public class CameraRig : MonoBehaviour
{
    [SerializeField] 
    private CinemachineGroupFraming targetGroup;

    [SerializeField]
    private float zoomPadding;
    
    private float _minZoom;
    private float _activeZoom;

    private float _yRange;
    
    private void Awake()
    {
        if (targetGroup == null)
        {
            targetGroup = GetComponent<CinemachineGroupFraming>();
        }

        PlayerController.ShipGridUpdate += UpdateCameraZoom;
        PlayerController.PlayerSpawnedEvent += OnPlayerSpawn;

        _minZoom = targetGroup.OrthoSizeRange.x;
        _yRange = targetGroup.OrthoSizeRange.y;
    }

    private void OnDestroy()
    {
        PlayerController.ShipGridUpdate -= UpdateCameraZoom;
        PlayerController.PlayerSpawnedEvent -= OnPlayerSpawn;
    }

    private void OnPlayerSpawn(PlayerController obj)
    {
        UpdateCameraZoom(obj.GetShipBounds());
    }

    private void UpdateCameraZoom(Bounds bounds)
    {
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y);
        
        _activeZoom = Mathf.Max(_minZoom, maxSize +  zoomPadding);
        
        targetGroup.OrthoSizeRange = new Vector2(_activeZoom, _yRange);
    }
}
}