using System.Collections;
using UnityEngine;

/// <summary>
/// Represents a weapon
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    protected GameObject projectilePrefab;

    [SerializeField]
    protected Collider2D parentCollider;

    [SerializeField]
    protected WeaponStrategy weaponStrategy;

    [SerializeField]
    protected Transform firePoint;

    [field: SerializeField]
    public float FireRatePerSecond { get; protected set; } = 0.5f;

    private Coroutine _fireTimerRoutine;

    private Projectile _projectile;

    public bool CanFire => _fireTimerRoutine == null;

    private void Start()
    {
        _projectile = projectilePrefab.gameObject.GetComponent<Projectile>();
    }

    /// <summary>
    /// Setter for weapon strategy
    /// </summary>
    /// <param name="strat"></param>
    private void SetWeaponStrategy(WeaponStrategy strat)
    {
        weaponStrategy = strat;
    }

    private GameObject _spawnProjectile(Transform spawnPoint) =>
        _projectile.SpawnProjectile(spawnPoint, parentCollider);

    public virtual void Fire()
    {
        if (!CanFire)
        {
            return;
        }

        weaponStrategy.Fire(firePoint, this, _spawnProjectile);
        StartFireTimer();
    }

    private void StartFireTimer()
    {
        _fireTimerRoutine = StartCoroutine(ActivateFireTimer());
    }

    private IEnumerator ActivateFireTimer()
    {
        yield return new WaitForSeconds(1 / FireRatePerSecond);
        _fireTimerRoutine = null;
    }
}