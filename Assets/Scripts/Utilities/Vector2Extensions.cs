using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2Int ToVector2Int(this Vector2 vector2) => new ((int)vector2.x, (int)vector2.y);
    public static Vector2 ToVector2(this Vector2Int vector2) => new (vector2.x, vector2.y);
    public static Vector2Int ToVector2Int(this Vector3 vector3) => new ((int)vector3.x, (int)vector3.y);

    /// <summary>
    /// Adds the given value to both x and y of the Vector2
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Vector2 Add(ref this Vector2 vec, float value)
    {
        vec.x += value;
        vec.y += value;
        return vec;
    }
}
