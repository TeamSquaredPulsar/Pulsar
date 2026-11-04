using UnityEngine;

namespace Pulsar.Interfaces
{
public interface IThrustable
{
    float Thrust { get; }

    void Move(float amount, ref Rigidbody2D targetRb)
    {
        Debug.Log($"I want to move {amount}");
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }
}
}
