using Pulsar.Interfaces;
using UnityEngine;

public class CoreTile : Tile, IThrustable
{
    [field: SerializeField]
    [Min(0f)]
    public float ThrustForce { get; private set; } = 0.1f;

    [field: SerializeField]
    [Min(0f)]
    public float RotationForce { get; private set; } = 0.1f;

    public void Activate()
    {
    }

    public void Deactivate()
    {
        //TODO: Whatever logic we need to do here
    }
}