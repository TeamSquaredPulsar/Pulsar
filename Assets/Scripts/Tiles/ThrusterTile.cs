using UnityEngine;

public class ThrusterTile : Tile
{
    
    [field: SerializeField]
    public float Thrust { get; private set; } = 0.1f;
        
    public void Move(float amount, ref Rigidbody2D parentRb)
    {
        Debug.Log($"I want to move {amount}");
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }
}