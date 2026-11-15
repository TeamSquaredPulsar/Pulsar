using System;
using Pulsar.Audio;
using UnityEngine;

namespace Pulsar.Weapons.Strategies
{
/// <summary>
/// Shoots a single bullet at a time
/// </summary>
[CreateAssetMenu(fileName = "DoubleShot",
    menuName = "Scriptable Objects/DoubleShot")]
public class DoubleShot : WeaponStrategy
{
    private const int k_LeftFirePoint = 0;
    private const int k_RightFirePoint = 1;
    private const int k_MaxFirePoints = 2;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, k_MaxFirePoints);

        spawnProjectile(firePoints[k_LeftFirePoint]);
        spawnProjectile(firePoints[k_RightFirePoint]);
        AudioManager.Instance.PlayOneShot(weaponFiredSound,
            firePoints[k_LeftFirePoint].position);
    }
}
}
