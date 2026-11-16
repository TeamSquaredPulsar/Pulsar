using System.Collections;
using NaughtyAttributes;
using Pulsar.Hazards;
using Pulsar.Interactions;
using Pulsar.Weapons.Strategies;
using UnityEngine;

namespace Pulsar.Weapons
{
/// <summary>
/// Represents a weapon
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
public abstract class Weapon : Tile, IProjectileSpawner, IWeapon
{
    [field: SerializeField]
    public float FireRatePerSecond { get; protected set; } = 0.5f;

    [field: SerializeField]
    public Sprite ReloadingSprite { get; private set; }

    private Coroutine _fireTimerRoutine;
    private Projectile _projectile;

    protected override void Start()
    {
        base.Start();
        _projectile = ProjectilePrefab.gameObject.GetComponent<Projectile>();
    }

    [Header("References")]
    [field: SerializeField]
    public GameObject ProjectilePrefab { get; private set; }

    Collider2D IProjectileSpawner.ParentCollider => ParentCollider;

    public Collider2D Coll2D => tileCollider;

    public GameObject SpawnProjectile(Transform spawnPoint)
    {
        GameObject projectile
            = _projectile.SpawnProjectile(spawnPoint, ParentCollider);
        Physics2D.IgnoreCollision(projectile.GetComponent<Collider2D>(),
            Coll2D);
        return projectile;
    }

    [field: SerializeField]
    public Collider2D ParentCollider { get; private set; }

    [field: SerializeField]
    [field: ReorderableList]
    public Transform[] FirePoints { get; private set; }

    [field: SerializeField]
    [field: Expandable]
    public WeaponStrategy Strategy { get; private set; }

    public bool CanFire => _fireTimerRoutine == null;

    public void Fire()
    {
        if (!CanFire)
        {
            return;
        }

        Strategy.Fire(FirePoints, this, SpawnProjectile);
        StartFireTimer();
    }

    private void StartFireTimer()
    {
        _fireTimerRoutine = StartCoroutine(ActivateFireTimer());
        RefreshSprite();
    }

    private IEnumerator ActivateFireTimer()
    {
        yield return new WaitForSeconds(1 / FireRatePerSecond);
        _fireTimerRoutine = null;
        RefreshSprite();
    }

    public void SetParentCollider(Collider2D newCollider)
    {
        ParentCollider = newCollider;
    }

    protected override Sprite GetCurrentSprite(HealthState state) =>
        ReloadingSprite && !CanFire
            ? ReloadingSprite
            : base.GetCurrentSprite(state);
}
}
