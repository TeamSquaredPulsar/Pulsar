using UnityEngine;

namespace Pulsar.DataClasses
{
[CreateAssetMenu(menuName = "CameraData")]
public class CameraData : ScriptableObject
{
    /*
     * Use this if you want to keep a reference to the camera, and listen for
     * changes rather than getting the camera every time you need it.
     * It's likely a small efficiency gain; but if constantly getting the
     * MainCamera winds up being a bottleneck somewhere, I wanted to have this
     * as an easy win.
     * If you do uncomment this, make it get called when MainCamera is set
     */
    // public static event Action<Camera> MainCameraChanged;

    public Camera MainCamera { get; set; }

    public Vector2 ScreenSize => MainCamera.pixelRect.size;
}
}