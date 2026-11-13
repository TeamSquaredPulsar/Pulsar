using Pulsar.DataClasses;
using UnityEngine;

namespace Pulsar.Interfaces
{
public interface IThrustable
{
    float Thrust { get; }

    TileRotation Rotation { get; }

    void Activate(float amount, in Rigidbody2D targetRb)
    {
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }

    void Deactivate();
}
}
