using Audune.Utils.Dictionary;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using Pulsar.Weapons;
using Pulsar.Weapons.Strategies;
using UnityEngine;

namespace Pulsar.Player
{
/// <summary>
/// Special Core Tile to allow the player to have a pintle weapon as their core.
/// @author Corey Buchan
/// @version 0.9
/// </summary>
public class PlayerCore : AutoLazerWeapon, IThrustable, IShipCore
{
    [field:SerializeField]
    [field:SerializableDictionaryOptions(keyHeader = "Direction",
        valueHeader = "Force")]
    public SerializableDictionary<TileRotation, float> DirectionalThrust
    {
        get;
        private set;
    }

    [field: SerializeField]
    public float RotationForce { get; private set; }

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
}
