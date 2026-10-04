using UnityEngine;

namespace Pulsar.Ship
{
    public static class ShipUtilities
    {
        public const float TileColliderHeight = 0.2f;
        public static Vector3 GridToLocal(Vector2 cell) => new(cell.x, 0f, cell.y);
        public static Vector2 LocalToGrid(Vector3 local) => new(local.x, local.z);
        public static Quaternion RotateQuarterOnYAxis(int rotation) => Quaternion.Euler(0f, -90f * rotation, 0f);

        public static void Constrain(Rigidbody body)
        {
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY
                               | RigidbodyConstraints.FreezeRotationX
                               | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}
