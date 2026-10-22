using UnityEngine;

namespace Pulsar.Ship.Ship.Extensions
{
public static class Vector2Extensions
{
    public static Vector2Int ToVector2Int(this Vector2 vector2) => new ((int)vector2.x, (int)vector2.y);
    public static Vector2 ToVector2(this Vector2Int vector2) => new (vector2.x, vector2.y);
    public static Vector2Int ToVector2Int(this Vector3 vector3) => new ((int)vector3.x, (int)vector3.y);
}
}