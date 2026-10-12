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
    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        if (firePoints == null || firePoints.Length == 0)
        {
            return;
        }

        spawnProjectile(firePoints[0]);
        spawnProjectile(firePoints[1]);
        spawnProjectile(firePoints[2]);
    }
}