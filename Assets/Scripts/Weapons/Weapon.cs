using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Pulsar.Hazards;
using Pulsar.Interactions;
using Pulsar.Weapons.Strategies;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pulsar.Weapons
{
/// <summary>
///     Represents a weapon
///     Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///     @author Alfredo Luzardo
///     @version 1.1
/// </summary>
public abstract class Weapon : Tile, IProjectileSpawner, IWeapon
{
    [field: SerializeField]
    public float FireRatePerSecond { get; protected set; } = 0.5f;

    [field: SerializeField]
    public Sprite ReloadingSprite { get; private set; }

    [FormerlySerializedAs("ps")]
    [SerializeField]
    private ParticleSystem muzzleEffectPrefab;

    private readonly Dictionary<Transform, ParticleSystem> _fireEffectMap
        = new();

    private Coroutine _fireTimerRoutine;
    private Projectile _projectile;

    protected override void Start()
    {
        base.Start();
        _projectile = ProjectilePrefab
            ? ProjectilePrefab.GetComponent<Projectile>()
            : null;

        // passing the muzzle particle effect as a child to each fire point
        if (muzzleEffectPrefab == null)
        {
            return;
        }

        foreach (Transform firePoint in FirePoints)
        {
            ParticleSystem effect = Instantiate(muzzleEffectPrefab, firePoint);
            _fireEffectMap[firePoint] = effect;
        }
    }

    [Header("References")]
    [field: SerializeField]
    public GameObject ProjectilePrefab { get; private set; }

    Collider2D IProjectileSpawner.ParentCollider => ParentCollider;

    public Collider2D Coll2D => tileCollider;

    public virtual GameObject SpawnProjectile(Transform spawnPoint)
    {
        GameObject projectile
            = _projectile.SpawnProjectile(spawnPoint, ParentCollider);
        Physics2D.IgnoreCollision(projectile.GetComponent<Collider2D>(),
            Coll2D);

        bool effectFound
            = _fireEffectMap.TryGetValue(spawnPoint, out ParticleSystem effect);

        if (effectFound)
        {
            effect.Play();
        }

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

    public virtual void Fire()
    {
        if (!CanFire)
        {
            return;
        }

        Strategy.Fire(FirePoints, this, SpawnProjectile);
        StartFireTimer();
    }

    private void StartFireTimer() => StartCooldown(1 / FireRatePerSecond);

    protected void StartCooldown(float seconds)
    {
        _fireTimerRoutine = StartCoroutine(ActivateFireTimer(seconds));
        RefreshSprite();
    }

    private IEnumerator ActivateFireTimer(float seconds)
    {
        yield return new WaitForSeconds(seconds);
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
