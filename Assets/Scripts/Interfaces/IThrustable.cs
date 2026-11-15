using Pulsar.DataClasses;
using UnityEngine;

namespace Pulsar.Interfaces
{
/// <summary>
/// Interface for any tile that can apply thrust to a rigidbody.
/// Doesn't have to be a ship!
/// @author Corey Buchan
/// @version 0.9
/// </summary>
public interface IThrustable
{
    float ThrustForce { get; }
    float RotationForce { get; }

    TileRotation Rotation { get; }

    void Activate(float amount, in Rigidbody2D targetRb)
    {
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }

    void Deactivate();
}
}