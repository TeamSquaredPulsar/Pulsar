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
    private float k_MiddleAngle = 0f;

    [SerializeField]
    private float k_RightAngle = 45f;

    [SerializeField]
    private float k_DefaultRotation = 0f;

    /// <summary>
    /// Shoots three times at different angles
    /// </summary>
    /// <param name="firePoint"></param>
    public override void Fire(Transform firePoint, Weapon weapon)
    {
        Shoot(firePoint, k_LeftAngle);
        Shoot(firePoint, k_MiddleAngle);
        Shoot(firePoint, k_RightAngle);
    }

    /// <summary>
    /// Helper method, so that I can specify the angle for each one easier
    /// cut down code
    /// </summary>
    /// <param name="firePoint"></param>
    /// <param name="angle"></param>
    private void Shoot(Transform firePoint, float angle)
    {
        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation * Quaternion.Euler(
                k_DefaultRotation,
                k_DefaultRotation,
                angle)
        );

        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        projectileComponent.SetSpeed(projectileSpeed);
    }
}