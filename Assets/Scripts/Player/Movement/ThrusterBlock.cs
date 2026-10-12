using UnityEngine;

public enum ThrusterOrientation
{
    Forward = 0,
    Backward = 1,
    Left = 2,
    Right = 3
}

public class ThrusterBlock : MonoBehaviour
{
    [SerializeField]
    private float thrust;

    [SerializeField]
    private ThrusterOrientation orientation = ThrusterOrientation.Forward;

    private float defaultSpeed = 0.1f;

    private void Awake()
    {
        if (thrust <= 0)
        {
            Debug.LogError("No thrust set");
            thrust = defaultSpeed;
        }
    }

    public float GetThrust()
    {
        return thrust;
    }

    public ThrusterOrientation GetOrientation()
    {
        return orientation;
    }
}