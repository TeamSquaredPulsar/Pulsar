using System;
using UnityEngine;

/// <summary>
/// Shoots three bullets at a time
///
/// @author Alfredo Luzardo
/// @version 1.0
/// </summary>
[CreateAssetMenu(fileName = "TripleShot",
    menuName = "Scriptable Objects/TripleShot")]
public class TripleShot : WeaponStrategy
{
    private int _maxBullets = 3;
    private int k_max_firePoints = 3;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        WeaponTile weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, k_max_firePoints);

        foreach (Transform firePoint in firePoints[.._maxBullets])
        {
            spawnProjectile(firePoint);
        }
    }
}