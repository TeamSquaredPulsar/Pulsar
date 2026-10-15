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
    private int k_BulletStart;

    [SerializeField]
    private int k_MaxBullets = 3;

    [SerializeField]
    private float k_bulletGapTime = 0.1f;

    private int k_max_firePoints = 1;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        WeaponTile weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        verifyFirePoints(firePoints, k_max_firePoints);

        weapon.StartCoroutine(Burst(firePoints, spawnProjectile));
    }

    /// <summary>
    /// Burst method, IEnumerator for delaying time
    /// </summary>
    /// <param name="firePoint"></param>
    /// <param name="spawnProjectile"></param>
    /// <returns></returns>
    private IEnumerator Burst(
        Transform[] firePoints,
        Func<Transform, GameObject> spawnProjectile)
    {
        foreach (Transform firePoint in firePoints[..k_MaxBullets])
        {
            spawnProjectile(firePoint);
            yield return new WaitForSeconds(k_bulletGapTime);
        }
    }
}