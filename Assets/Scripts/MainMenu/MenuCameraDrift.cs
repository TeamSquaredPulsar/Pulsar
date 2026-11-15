using Pulsar.DataClasses;
using UnityEngine;

namespace Pulsar.UI
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-100)]
    public class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private CameraData cameraData;
        [SerializeField] private Vector2 velocity = new Vector2(0.5f, 0.15f);

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            cameraData.MainCamera = _camera;
        }

        private void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (cameraData.MainCamera == _camera)
            {
                cameraData.MainCamera = null;
            }
        }
    }
}
