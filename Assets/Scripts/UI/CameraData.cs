using System;
using UnityEngine;

[CreateAssetMenu(menuName = "CameraData")]
public class CameraData : ScriptableObject
{
    public event Action<Camera> MainCameraChanged;
    public Camera MainCamera
    {
        get => _mainCamera;
        set
        {
            _mainCamera = value;
            MainCameraChanged?.Invoke(value);
        }
    }

    private Camera _mainCamera;
    
    public Vector2 ScreenSize => new (
        MainCamera.pixelWidth,
        MainCamera.pixelHeight
    );

    private void Awake()
    {
        if (MainCamera)
        {
            return;
        }

        if (!Camera.main)
        {
            Debug.LogError("No main camera found!");
        }
        else
        {
            MainCamera = Camera.main;
        }
    }
}