using System;
using Pulsar.Interfaces;
using UnityEngine;

public class CoreTile : Tile, IThrustable, IShipCore
{
    public event Action OnCoreDestroyed;

    [field: SerializeField]
    [Min(0f)]
    public float ThrustForce { get; private set; } = 0.1f;

    [field: SerializeField]
    [Min(0f)]
    public float RotationForce { get; private set; } = 0.1f;

    protected override void Awake()
    {
        base.Awake();
        DestroyEvent += _ => OnCoreDestroyed?.Invoke();
    }

    public void ActivateThrust(float amount, in Rigidbody2D targetRb)
    {
    }

    public void DeactivateThrust()
    {
    }

    public void ActivateRotation(float amount, in Rigidbody2D targetRb)
    {
    }

    public void DeactivateRotation()
    {
    }
}
