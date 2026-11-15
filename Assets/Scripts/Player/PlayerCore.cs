using System;
using Pulsar.Interfaces;
using Pulsar.Weapons;
using UnityEngine;

namespace Pulsar
{
/// <summary>
/// Special Core Tile to allow the player to have a pintle weapon as their core.
/// @author Corey Buchan
/// @version 0.9
/// </summary>
public class PlayerCore : AutoWeapon, IThrustable, IShipCore
{
    public event Action OnCoreDestroyed;

    [field: SerializeField]
    public float ThrustForce { get; private set; }

    [field: SerializeField]
    public float RotationForce { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        DestroyEvent += _ => OnCoreDestroyed?.Invoke();
    }

    public void Activate(float amount, in Rigidbody2D targetRb)
    {
        spriteRenderer.color = Color.red;
    }

    public void Deactivate()
    {
        spriteRenderer.color = Color.white;
    }
}
}
