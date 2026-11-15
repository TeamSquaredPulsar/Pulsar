using System;
using UnityEngine;

/// <summary>
/// Shoots a single bullet at a time
/// </summary>
[CreateAssetMenu(fileName = "DoubleShot",
    menuName = "Scriptable Objects/DoubleShot")]
public class DoubleShot : WeaponStrategy
{
    private const int LeftFirePoint = 0;
    private const int RightFirePoint = 1;
    private const int MaxFirePoints = 2;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, MaxFirePoints);

        spawnProjectile(firePoints[LeftFirePoint]);
        spawnProjectile(firePoints[RightFirePoint]);
    }
}