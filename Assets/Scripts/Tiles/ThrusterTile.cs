using Pulsar.Ship.Extensions;
using UnityEngine;

public class ThrusterTile : Tile
{
    
    [field: SerializeField]
    public float Thrust { get; private set; } = 0.1f;


    public TileRotationEnum Orientation => Rotation.ToRotationEnum();
        
    public void Move(float amount, Rigidbody2D rb)
    {
        Debug.Log($"I want to move {amount}");
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }
}