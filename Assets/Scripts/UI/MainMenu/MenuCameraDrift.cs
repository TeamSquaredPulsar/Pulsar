using Pulsar.DataClasses;
using UnityEngine;

namespace Pulsar.UI
{
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-100)]
public class MenuCameraDrift : MonoBehaviour
{
    [SerializeField]
    private Vector2 velocity = new Vector2(0.5f, 0.15f);

    [SerializeField]
    private Camera cam;

    private void Start()
    {
        CameraData.MainCamera = cam;
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (CameraData.MainCamera == cam)
        {
            CameraData.MainCamera = null;
        }
    }
}
}
