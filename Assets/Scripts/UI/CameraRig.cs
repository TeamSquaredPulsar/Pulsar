using System;
using Pulsar.Ship;
using Pulsar.Ship.Data;
using UnityEngine;
using Unity.Cinemachine;

public class CameraRig : MonoBehaviour
{
    [SerializeField] 
    private CinemachineGroupFraming targetGroup;
    
    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private float zoomPadding;
    
    private float minZoom;
    private float activeZoom;

    private float yRange;
    
    private void Awake()
    {
        if (targetGroup == null)
        {
            targetGroup = GetComponent<CinemachineGroupFraming>();
        }

        minZoom = targetGroup.OrthoSizeRange.x;
        yRange = targetGroup.OrthoSizeRange.y;
    }

    private void OnEnable()
    {
        shipManager.ShipGridUpdate += UpdateCameraZoom;
    }

    private void OnDisable()
    {
        shipManager.ShipGridUpdate -= UpdateCameraZoom;
    }

    private void Start()
    {
        UpdateCameraZoom();
    }

    private void UpdateCameraZoom()
    {
        Bounds bounds = shipManager.GetLocalBounds();
        
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y);
        
        activeZoom = Mathf.Max(minZoom, maxSize +  zoomPadding);
        
        targetGroup.OrthoSizeRange = new Vector2(activeZoom, yRange);
    }
}
