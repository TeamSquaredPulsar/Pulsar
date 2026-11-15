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
    private int maxBullets = 3;

    [SerializeField]
    private float k_bulletGapTime = 0.1f;

    private const int k_MaxFirePoints = 1;

    /// <inheritdoc/>
    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, k_MaxFirePoints);

        weapon.StartCoroutine(Burst(firePoints, spawnProjectile));
    }

    /// <summary>
    /// Burst method, IEnumerator for delaying time
    /// </summary>
    /// <param name="firePoints"></param>
    /// <param name="spawnProjectile"></param>
    /// <returns></returns>
    private IEnumerator Burst(
        Transform[] firePoints,
        Func<Transform, GameObject> spawnProjectile)
    {
        for (int i = 0; i < maxBullets; i++)
        {
            spawnProjectile(firePoints[i % firePoints.Length]);
            yield return new WaitForSeconds(k_bulletGapTime);
        }
    }
}