using System;
using Pulsar.Audio;
using UnityEngine;

namespace Pulsar.Weapons.Strategies
{
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
    private const int k_MainFirePoint = 0;
    private const int k_MaxFirePoints = 1;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, k_MaxFirePoints);

        spawnProjectile(firePoints[k_MainFirePoint]);
        AudioManager.Instance.PlayOneShot(
            FmodEvents.Instance.ShootLight,
            firePoints[k_MainFirePoint].position);
    }
}
}
