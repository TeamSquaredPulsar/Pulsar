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

    public static TileRotationEnum Add(this TileRotationEnum rot, int value) =>
        (rot.ToInt() + value).ToRotationEnum();

    public static TileRotationEnum Add(
        this TileRotationEnum self,
        TileRotationEnum value) => self.Add(value.ToInt());

    public static TileRotationEnum Opposite(this TileRotationEnum rot)
    {
        int half = Enum.GetNames(typeof(TileRotationEnum)).Length / 2;
        return (rot.ToInt() + half).ToRotationEnum();
    }

    public static int ToInt(this TileRotationEnum rot) => (int)rot;

    public static TileRotationEnum PlusInt(
        this TileRotationEnum current,
        int rot) => (current.ToInt() + rot).ToRotationEnum();
}
}