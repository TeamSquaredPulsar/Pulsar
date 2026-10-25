using System;
using UnityEngine;

[CreateAssetMenu(menuName = "CameraData")]
public class CameraData : ScriptableObject {
    
    public Camera mainCamera;
    
    public Vector2 ScreenSize => new (
        mainCamera.pixelWidth,
        mainCamera.pixelHeight
    );

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        if (mainCamera != null)
        {
            Debug.Log("Disabling main camera!");
        }
    }
}