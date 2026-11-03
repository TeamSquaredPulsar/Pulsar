using Pulsar.DataClasses;
using Pulsar.DataClasses.Extensions;
using UnityEngine;

public class ThrusterTile : Tile
{
    
    [field: SerializeField]
    public float Thrust { get; private set; } = 0.1f;

    public Vector2 LocalThrustDirection => Rotation.ToVector2Int();
    
    public void Move(ref Rigidbody2D parentRb)
    {
        Vector2 direction = transform.rotation * (Thrust * Vector2.up);
        parentRb.AddForceAtPosition(direction, transform.position, 
                                                            ForceMode2D.Force);
    }
}