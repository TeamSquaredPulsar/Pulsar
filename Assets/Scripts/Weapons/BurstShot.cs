using UnityEngine;
using System.Collections;

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
    private int k_BulletStart = 0;

    [SerializeField]
    private float k_bulletGapTime = 0.1f;

    /// <summary>
    /// Fire method
    /// </summary>
    /// <param name="firePoint"></param>
    public override void Fire(Transform firePoint, Weapon weapon)
    {
        weapon.StartCoroutine(Burst(firePoint));
    }

    /// <summary>
    /// Burst method, IEnumerator for delaying time
    /// </summary>
    /// <param name="firePoint"></param>
    /// <returns></returns>
    private IEnumerator Burst(Transform firePoint)
    {
        for (int i = k_BulletStart; i < k_MaxBullets; i++)
        {
            if (firePoint == null)
            {
                yield break;
            }

            GameObject bullet = Instantiate(
                projectilePrefab, firePoint.position, firePoint.rotation);

            bullet.GetComponent<Projectile>().SetSpeed(projectileSpeed);

            yield return new WaitForSeconds(k_bulletGapTime);
        }
    }
}