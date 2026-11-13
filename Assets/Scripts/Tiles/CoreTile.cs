using Pulsar.Interfaces;
using UnityEngine;

public class CoreTile : Tile, IThrustable
{
    [field: SerializeField]
    public float Thrust { get; private set; } = 0.1f;

    public void Activate()
    {
        
    }
    public void Deactivate()
    {
        //TODO: Whatever logic we need to do here
    }
}