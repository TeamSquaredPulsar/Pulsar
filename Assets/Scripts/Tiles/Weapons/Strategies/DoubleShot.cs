using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shoots a single bullet at a time
/// </summary>
[CreateAssetMenu(fileName = "DoubleShot",
    menuName = "Scriptable Objects/DoubleShot")]
public class DoubleShot : WeaponStrategy
{
    private int k_leftFirePoint = 0;
    private int k_rightFirePoint = 1;
    private int k_max_firePoints = 2;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        if (firePoints == null || firePoints.Length < k_max_firePoints)
        {
            return;
        }

        spawnProjectile(firePoints[k_leftFirePoint]);
        spawnProjectile(firePoints[k_rightFirePoint]);
    }
}