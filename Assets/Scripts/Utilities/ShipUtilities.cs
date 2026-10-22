using UnityEngine;

public static class ShipUtilities
{

    public static Vector3 GridToLocal(Vector2 cell) => new(cell.x, cell.y, 0f);
    public static Vector2 LocalToGrid(Vector3 local) => new(local.x, local.y);

    public static Quaternion RotateQuarterOnZAxis(int rotation) =>
        Quaternion.Euler(0f, 0f, 90f * rotation);
}