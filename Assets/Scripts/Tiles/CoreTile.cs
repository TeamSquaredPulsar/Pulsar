using UnityEngine;

public class CoreTile : Tile
{
    [field: SerializeField]
    public float CoreThrust { get; private set; } = 2f;

    public void ApplyThrust(Rigidbody2D rb, Vector2 moveInput)
    {
        if (moveInput == Vector2.zero)
        {
            return;
        }
        
        Vector2 force = transform.TransformDirection(moveInput * CoreThrust);
        rb.AddForceAtPosition(force, rb.position, ForceMode2D.Force);
    }

}