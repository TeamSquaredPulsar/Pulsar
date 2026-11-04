using UnityEngine;

namespace Pulsar.Weapons
{
public interface IProjectileSpawner
{
    Collider2D Coll2D { get; }
    GameObject ProjectilePrefab { get; }
    
    Collider2D ParentCollider { get; }

    GameObject SpawnProjectile(Transform spawnPoint);
}
}