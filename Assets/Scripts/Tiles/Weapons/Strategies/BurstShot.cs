using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Represents a BurstShot
///
/// Shoots bullets in a burst
/// 
/// @author Alfredo Luzardo
/// @version 1.0
/// </summary>
[CreateAssetMenu(fileName = "BurstShot",
    menuName = "Scriptable Objects/BurstShot")]
public class BurstShot : WeaponStrategy
{
    [SerializeField]
    private int k_MaxBullets = 3;

    [SerializeField]
    private int k_BulletStart;

    [SerializeField]
    private float k_bulletGapTime = 0.1f;

    /// <inheritdoc/>
    public override void Fire(
        Transform firePoint,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        weapon.StartCoroutine(Burst(firePoint, spawnProjectile));
    }

    /// <summary>
    /// Burst method, IEnumerator for delaying time
    /// </summary>
    /// <param name="firePoint"></param>
    /// <param name="spawnProjectile"></param>
    /// <returns></returns>
    private IEnumerator Burst(
        Transform firePoint,
        Func<Transform, GameObject> spawnProjectile)
    {
        for (int i = k_BulletStart; i < k_MaxBullets; i++)
        {
            if (firePoint == null)
            {
                yield break;
            }

            spawnProjectile(firePoint);

            yield return new WaitForSeconds(k_bulletGapTime);
        }
    }
}