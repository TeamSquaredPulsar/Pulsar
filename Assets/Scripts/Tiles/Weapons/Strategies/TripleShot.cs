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
    [SerializeField]
    private float k_LeftAngle = -45f;

    [SerializeField]
    private float k_MiddleAngle;

    [SerializeField]
    private float k_RightAngle = 45f;

    /// <inheritdoc/>
    public override void Fire(
        Transform firePoint,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        Shoot(firePoint, k_LeftAngle, spawnProjectile);
        Shoot(firePoint, k_MiddleAngle, spawnProjectile);
        Shoot(firePoint, k_RightAngle, spawnProjectile);
    }

    /// <summary>
    /// Helper method, so that I can specify the angle for each one easier
    /// cut down code
    /// </summary>
    /// <param name="firePoint"></param>
    /// <param name="angle"></param>
    /// <param name="spawnProjectile"></param>
    private void Shoot(
        Transform firePoint,
        float angle,
        Func<Transform, GameObject> spawnProjectile)
    {
        Transform newPoint = firePoint;
        newPoint.Rotate(new Vector3(0, 0, angle));
        spawnProjectile(newPoint);
    }
}