using System;
using Pulsar.Ship;
using Pulsar.Ship.Data;
using UnityEngine;
using Unity.Cinemachine;

public class CameraRig : MonoBehaviour
{
    [SerializeField] 
    private CinemachinePositionComposer positionComposer;
    
    [SerializeField]
    private ShipManager shipManager;
    
    private float minZoom;
    
    private void Awake()
    {
        if (positionComposer == null)
        {
            positionComposer = GetComponent<CinemachinePositionComposer>();
        }
        
        minZoom = positionComposer.CameraDistance;
    }

    private void Start()
    {
        
    }
}
