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
public class PlayerCore : PintleWeaponTile, IThrustable, IShipCore
{
    public event Action OnCoreDestroyed;

    [field: SerializeField]
    public float Thrust { get; private set; }

    private void Awake()
    {
        DestroyEvent += (_) => OnCoreDestroyed?.Invoke();
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