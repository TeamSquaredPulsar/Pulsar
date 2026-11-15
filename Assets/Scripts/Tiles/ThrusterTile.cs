using Pulsar.Interfaces;
using UnityEngine;

public class ThrusterTile : Tile, IThrustable
{
    [field: SerializeField]
    [Min(0f)]
    public float ThrustForce { get; private set; } = 0.1f;

    [field: SerializeField]
    [Min(0f)]
    public float RotationForce { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        DeactivateThrust();
    }

    public void ActivateThrust(float amount, in Rigidbody2D targetRb)
    {
        spriteRenderer.color = Color.red;
    }

    public void DeactivateThrust()
    {
        spriteRenderer.color = Color.white;
    }

    public void ActivateRotation(float amount, in Rigidbody2D targetRb)
    {
    }

    public void DeactivateRotation()
    {
    }
}
