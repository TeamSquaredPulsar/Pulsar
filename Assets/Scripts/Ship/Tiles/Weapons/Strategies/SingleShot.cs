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
    /// <summary>
    /// Fire method
    /// </summary>
    /// <param name="firePoint"></param>
    /// <param name="weaponTile"></param>
    /// <param name="spawnProjectile"></param>
    public override void Fire(
        Transform firePoint,
        WeaponTile weaponTile,
        Func<Transform, GameObject> spawnProjectile)
    {
        spawnProjectile(firePoint);
    }
}