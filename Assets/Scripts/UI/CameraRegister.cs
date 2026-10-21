using UnityEngine;

public class CameraRegister : MonoBehaviour {
    [SerializeField] private CameraData cameraDataAsset;

    private void Awake() {
        cameraDataAsset.mainCamera = Camera.main; // or GetComponent<Camera>()
    }
}