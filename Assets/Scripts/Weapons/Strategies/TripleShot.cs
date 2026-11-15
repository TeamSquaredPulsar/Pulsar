using System;
using UnityEngine;

namespace Pulsar.Weapons.Strategies
{
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
    private const int k_MaxBullets = 3;
    private const int k_MaxFirePoints = 3;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, k_MaxFirePoints);

        foreach (Transform firePoint in firePoints[..k_MaxBullets])
        {
            spawnProjectile(firePoint);
        }
    }
}
}