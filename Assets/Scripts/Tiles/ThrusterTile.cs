using Pulsar.Interfaces;
using UnityEngine;

public class ThrusterTile : Tile, IThrustable
{
    
    [field: SerializeField]
    public float Thrust { get; private set; } = 0.1f;



    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Deactivate();
        
    }
    
    public void Activate(float amount, ref Rigidbody2D parentRb)
    {
        _spriteRenderer.color = Color.red;
        Debug.Log($"I want to move {amount}");
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }
    
    public void Deactivate()
    {
        _spriteRenderer.color = Color.white;
    }
}