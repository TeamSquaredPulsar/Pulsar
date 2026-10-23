using System;
using UnityEngine;

namespace Pulsar.Ship.Extensions
{
public static class TileRotationExtensions
{
    public static TileRotation ToRotationEnum(this int rot)
    {
        int rotationPoints = Enum.GetNames(typeof(TileRotation))
            .Length;
        int direction = rot % rotationPoints;
        return (TileRotation)direction;
    }

    public static Vector2Int ToVector2Int(this TileRotation rot)
    {
        return rot switch
        {
            TileRotation.Up => Vector2Int.up,
            TileRotation.Right => Vector2Int.right,
            TileRotation.Down => Vector2Int.down,
            TileRotation.Left => Vector2Int.left,
            _ => throw new ArgumentOutOfRangeException(nameof(rot), rot,
                $"{rot} has to valid mapping for Vector2Int"),
        };
    }

    public static TileRotation Add(this TileRotation rot, int value) =>
        (rot.ToInt() + value).ToRotationEnum();

    public static TileRotation Add(
        this TileRotation self,
        TileRotation value) => self.Add(value.ToInt());

    public static TileRotation Opposite(this TileRotation rot)
    {
        int half = Enum.GetNames(typeof(TileRotation)).Length / 2;
        return (rot.ToInt() + half).ToRotationEnum();
    }

    public static int ToInt(this TileRotation rot) => (int)rot;

    public static TileRotation PlusInt(
        this TileRotation current,
        int rot) => (current.ToInt() + rot).ToRotationEnum();
}
}