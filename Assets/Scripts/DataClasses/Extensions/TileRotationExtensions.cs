using System;
using UnityEngine;

namespace Pulsar.DataClasses.Extensions
{
public static class TileRotationExtensions
{
    private const int FullCircleDegrees = 360;
    private static readonly int CircleSteps = FullCircleDegrees / Enum.GetNames(typeof(TileRotation))
        .Length;
    public static TileRotation ToTileRotation(this float rotDegrees)
    {
        int steps = Mathf.RoundToInt(rotDegrees / CircleSteps);
        return steps.ToTileRotation();
    }

    public static TileRotation ToTileRotation(this int rot)
    {
        int rotationPoints = Enum.GetNames(typeof(TileRotation))
            .Length;
        //Prevent from returning negative values
        int direction = ((rot % rotationPoints) 
                         + rotationPoints) % rotationPoints;
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

    private static TileRotation Add(this TileRotation rot, int value) =>
        (rot.ToInt() + value).ToTileRotation();

    public static TileRotation Add(
        this TileRotation self,
        TileRotation value) => self.Add(value.ToInt());

    private static TileRotation Subtract(this TileRotation self, int value) =>
        (self.ToInt() - value).ToTileRotation();

    public static TileRotation Subtract(
        this TileRotation self,
        TileRotation value) => self.Subtract(value.ToInt());

    public static TileRotation Opposite(this TileRotation rot)
    {
        int half = rot.Length() / 2;
        return (rot.ToInt() + half).ToTileRotation();
    }

    public static int ToInt(this TileRotation rot) => (int)rot;

    public static TileRotation PlusInt(
        this TileRotation current,
        int rot) => (current.ToInt() + rot).ToTileRotation();

    public static int Length<T>(this T enumValue) where T : Enum =>
        Enum.GetNames(typeof(T)).Length;
}
}