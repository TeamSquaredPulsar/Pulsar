using System;
using UnityEngine;

namespace Pulsar.Ship.Extensions
{
public static class TileRotationEnumExtensions
{
    public static TileRotationEnum ToRotationEnum(this int rot)
    {
        int rotationPoints = Enum.GetNames(typeof(TileRotationEnum))
            .Length;
        int direction = rot % rotationPoints;
        return (TileRotationEnum)direction;
    }

    public static Vector2Int ToVector2Int(this TileRotationEnum rot)
    {
        return rot switch
        {
            TileRotationEnum.Up => Vector2Int.up,
            TileRotationEnum.Right => Vector2Int.right,
            TileRotationEnum.Down => Vector2Int.down,
            TileRotationEnum.Left => Vector2Int.left,
            _ => throw new ArgumentOutOfRangeException(nameof(rot), rot,
                $"{rot} has to valid mapping for Vector2Int"),
        };
    }

    public static TileRotationEnum FromInt(this TileRotationEnum rot, int val)
    {
        int rotationPoints = Enum.GetNames(typeof(TileRotationEnum))
            .Length;
        int direction = val % rotationPoints;
        return (TileRotationEnum)direction;
    }

    public static int ToInt(this TileRotationEnum rot) => (int)rot;

    public static TileRotationEnum PlusInt(
        this TileRotationEnum current,
        int rot) => current.FromInt(current.ToInt() + rot);
}
}