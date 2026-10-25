using UnityEngine;

public class CameraRegister : MonoBehaviour {
    [SerializeField] private CameraData cameraDataAsset;
    [SerializeField] private Camera mainCamera;
    
    private void Awake() 
    {
        cameraDataAsset.mainCamera = mainCamera;
    }
}