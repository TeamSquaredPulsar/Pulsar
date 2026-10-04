using UnityEngine;

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
    /// <summary>
    /// Fire method
    /// </summary>
    /// <param name="firePoint"></param>
    /// <param name="layer"></param>
    public override void Fire(Transform firePoint, Weapon weapon)
    {
        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        projectileComponent.SetSpeed(projectileSpeed);
    }
}