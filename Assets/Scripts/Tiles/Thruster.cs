using UnityEngine;

public class Thruster : Tile
{
    [SerializeField]
    private float thrust = 0.1f;

    [SerializeField]
    private Rotation orientation = Rotation.Up;
    
    public float GetThrust() => thrust;

    public Rotation GetOrientation() => orientation;
    
    //public void Move(float amount, Rigidbody2D rb)
    //{
    //    Debug.Log($"I want to move {amount}");
    //    //TODO: Add movement logic here
    //    // Apply force to rigidbody at the point i exist at
    //}
}