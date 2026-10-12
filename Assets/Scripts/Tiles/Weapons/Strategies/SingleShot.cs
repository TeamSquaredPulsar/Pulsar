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
    private const int k_mainFirePoint = 0;

    /// <summary>
    /// Fire method
    /// </summary>
    /// <param name="firePoint"></param>
    /// <param name="weapon"></param>
    /// <param name="spawnProjectile"></param>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        if (firePoints == null || firePoints.Length == 0)
        {
            return;
        }

        spawnProjectile(firePoints[k_mainFirePoint]);
    }
}