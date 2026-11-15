using Audune.Utils.Dictionary;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using UnityEngine;

public class CoreTile : Tile, IThrustable, IShipCore
{
    [field: SerializeField]
    [field: SerializableDictionaryOptions(keyHeader = "Direction",
        valueHeader = "Force")]
    public SerializableDictionary<TileRotation, float> DirectionalThrust
    {
        get;
        private set;
    }

    [field: SerializeField]
    [Min(0f)]
    public float RotationForce { get; private set; } = 0.1f;

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
