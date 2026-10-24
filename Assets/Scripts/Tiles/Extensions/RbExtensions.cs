using UnityEngine;

public static class RbExtensions
{
    public static Rigidbody2D SetBuildRb(
        this Rigidbody2D original
    )
    {
        // original.simulated = false;
        original.linearVelocity = Vector2.zero;
        original.angularVelocity = 0f;
        original.bodyType = RigidbodyType2D.Kinematic;
        return original;
    }

    public static Rigidbody2D SetFloatRb(
        this Rigidbody2D original,
        Vector2 velocity,
        float angularVelocity, TileRbInfo rbInfo
    )
    {
        original.transform.SetParent(null, true);
        original.simulated = true;
        original.bodyType = RigidbodyType2D.Dynamic;
        original.constraints = RigidbodyConstraints2D.None;
        original.gravityScale = 0f;
        original.linearDamping = rbInfo.LinearDamping;
        original.angularDamping = rbInfo.AngularDamping;
        original.linearVelocity = velocity;
        original.angularVelocity = angularVelocity;
        return original;
    }
}