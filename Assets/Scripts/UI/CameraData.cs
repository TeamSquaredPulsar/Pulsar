using System;
using UnityEngine;

[CreateAssetMenu(menuName = "CameraData")]
public class CameraData : ScriptableObject
{
    public static event Action<Camera> MainCameraChanged;
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

    public Vector2 ScreenSize => MainCamera.pixelRect.size;
}