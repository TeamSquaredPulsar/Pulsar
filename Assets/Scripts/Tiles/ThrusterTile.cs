using UnityEngine;

public class ThrusterTile : Tile
{
    
    [SerializeField]
    private float thrust = 0.1f;
    
    public TileRotationEnum Orientation => GetRotationInt(Rotation);
    
    public float GetThrust() => thrust;
    
        
    public void Move(float amount, Rigidbody2D rb)
    {
        Debug.Log($"I want to move {amount}");
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }
}