using Pulsar.Interfaces;
using UnityEngine;

public class ThrusterTile : Tile, IThrustable
{
    
    [field: SerializeField]
    public float Thrust { get; private set; } = 0.1f;
}