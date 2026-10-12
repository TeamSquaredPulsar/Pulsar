using UnityEngine;

public enum ThrusterOrientation
{
    Forward = 0,
    Backward = 1,
    Left = 2,
    Right = 3,
}

public class Thruster : Tile
{
    [SerializeField]
    private float thrust = 0.1f;

    [SerializeField]
    private ThrusterOrientation orientation = ThrusterOrientation.Forward;
    
    public float GetThrust() => thrust;

    public ThrusterOrientation GetOrientation() => orientation;
    
    //public void Move(float amount, Rigidbody2D rb)
    //{
    //    Debug.Log($"I want to move {amount}");
    //    //TODO: Add movement logic here
    //    // Apply force to rigidbody at the point i exist at
    //}
}