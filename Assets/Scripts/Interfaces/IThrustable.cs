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

    void ActivateThrust(float amount, in Rigidbody2D targetRb);

    void DeactivateThrust();

    void ActivateRotation(float amount, in Rigidbody2D targetRb);
    void DeactivateRotation();
}
}
