using System.Collections;
using NaughtyAttributes;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Hazards;
using Pulsar.Weapons;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Represents a weapon
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
public class WeaponTile : Tile, IProjectileSpawner, IWeapon
{
    [Header("References")]
    [field: SerializeField]
    public GameObject ProjectilePrefab { get; private set; }

    [field: SerializeField]
    public Collider2D ParentCollider { get; private set; }

    [field: SerializeField, ReorderableList,]
    public Transform[] FirePoints { get; private set; }

    [field: SerializeField]
    public float FireRatePerSecond { get; protected set; } = 0.5f;

    [field: SerializeField, Expandable, FormerlySerializedAs("weaponStrategy"),]
    public WeaponStrategy Strategy { get; private set; }

    private Coroutine _fireTimerRoutine;
    private Projectile _projectile;
    Collider2D IProjectileSpawner.ParentCollider => ParentCollider;

    public Collider2D Coll2D => tileCollider;
    public bool CanFire => _fireTimerRoutine == null;

    private new void Start()
    {
        base.Start();
        _projectile = ProjectilePrefab.gameObject.GetComponent<Projectile>();
    }

    public GameObject SpawnProjectile(Transform spawnPoint)
    {
        GameObject projectile = _projectile.SpawnProjectile(spawnPoint, ParentCollider);
        Physics2D.IgnoreCollision(projectile.GetComponent<Collider2D>(),
            Coll2D);
        return projectile;
    }
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
    }

    private IEnumerator ActivateFireTimer()
    {
        yield return new WaitForSeconds(1 / FireRatePerSecond);
        _fireTimerRoutine = null;
    }

    public void SetParentCollider(Collider2D newCollider)
    {
        ParentCollider = newCollider;
    }
}