using UnityEngine;

public class ThrusterBlock : MonoBehaviour
{
    [SerializeField]
    private float thrust;

    [SerializeField]
    private Rotation orientation = Rotation.Up;

    [SerializeField]
    private float defaultSpeed = 0.1f;

    private void Awake()
    {
        if (thrust <= 0)
        {
            Debug.LogError("No thrust set");
            thrust = defaultSpeed;
        }
    }

    public float GetThrust() => thrust;

    public Rotation GetOrientation() => orientation;
}