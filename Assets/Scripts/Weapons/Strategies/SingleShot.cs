using System;
using UnityEngine;

/// <summary>
/// Shoots a single bullet at a time
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
[CreateAssetMenu(fileName = "SingleShot",
    menuName = "Scriptable Objects/SingleShot")]
public class SingleShot : WeaponStrategy
{
    private int k_mainFirePoint = 0;
    private int k_max_firePoints = 1;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        WeaponTile weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, k_max_firePoints);

        spawnProjectile(firePoints[k_mainFirePoint]);
    }
}