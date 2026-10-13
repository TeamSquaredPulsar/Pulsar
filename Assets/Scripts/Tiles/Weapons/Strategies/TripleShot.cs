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
    private int k_leftFirePoint = 0;
    private int k_mainFirePoint = 1;
    private int k_rightFirePoint = 2;

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

        spawnProjectile(firePoints[k_leftFirePoint]);
        spawnProjectile(firePoints[k_mainFirePoint]);
        spawnProjectile(firePoints[k_rightFirePoint]);
    }
}