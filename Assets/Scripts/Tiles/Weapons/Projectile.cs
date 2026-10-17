using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Represents a Projectile
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
/// 
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [field: SerializeField]
    public int Damage { get; protected set; } = 10;

    [SerializeField]
    protected float damageRadius = 1f;

    [SerializeField]
    protected float speed = 10f;

    [SerializeField]
    [Range(0.1f, 10f)]
    private float lifeTime = 4;

    [SerializeField]
    private GameObject hitEffectPrefab;

    private int ContactPointIndex = 0;

    /// <summary>
    /// Start method.
    /// </summary>
    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// When it collides, activate the hit effect, damage to the other,
    /// and destroy itself
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Bullets cant collide
        if (collision.gameObject.GetComponent<Projectile>())
        {
            return;
        }

        if (hitEffectPrefab != null)
        {
            Debug.Log("Hit Effect");
        }


        Health hp = GetColliderHealthComponent(collision);

        if (hp != null)
        {
            DealDamage(hp);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Deal Damage to what it hit
    /// </summary>
    /// <param name="health"></param>
    private void DealDamage(Health health)
    {
        health.TakeDamage(Damage);
    }

    /// <summary>
    /// Gets the health component of what the projectile hit
    /// </summary>
    /// <param name="colGameObject"></param>
    /// <param name="hitPoint"></param>
    /// <returns></returns>
    private Health GetColliderHealthComponent(Collision2D collision)
    {
        Health colliderHealth = null;
        Vector2 hitPoint = collision.GetContact(ContactPointIndex).point;

        if (collision.gameObject.TryGetComponent(out ShipManager shipManager))
        {
            Tile closestTile = GetClosestTile(shipManager, hitPoint);

            if (closestTile != null)
            {
                colliderHealth = closestTile.GetComponent<Health>();
            }
        }
        else if (collision.gameObject.TryGetComponent(out Health health))
        {
            colliderHealth = health;
        }

        return colliderHealth;
    }

    /// <summary>
    /// Gets the closest tile to the projectiles hit point on the ship
    /// </summary>
    /// <param name="shipManager"></param>
    /// <param name="hitPoint"></param>
    /// <returns></returns>
    private static Tile GetClosestTile(
        ShipManager shipManager,
        Vector2 hitPoint)
    {
        Tile closestTile = null;
        float bestDistanceSquared = float.PositiveInfinity;
        float bestCenterDistanceSquared = float.PositiveInfinity;

        foreach (Tile tile in shipManager.GetTiles())
        {
            Collider2D tileCollider = tile.GetComponent<Collider2D>();

            Vector2 closestPoint = tileCollider.ClosestPoint(hitPoint);
            float distanceSquared = (closestPoint - hitPoint).sqrMagnitude;
            float centerDistanceSquared =
                ((Vector2)tile.transform.position - hitPoint).sqrMagnitude;

            // Prefer the closest collider,
            // and if two tiles tie at a shared edge then
            // prefer the tile whose center is closest to the hit point
            if (distanceSquared < bestDistanceSquared ||
                (Mathf.Approximately(
                     distanceSquared, bestDistanceSquared) &&
                 centerDistanceSquared < bestCenterDistanceSquared))
            {
                closestTile = tile;
                bestDistanceSquared = distanceSquared;
                bestCenterDistanceSquared = centerDistanceSquared;
            }
        }

        return closestTile;
    }

    /// <summary>
    /// Spawns the projectile
    /// </summary>
    /// <param name="spawnTransform"></param>
    /// <param name="parentCollider"></param>
    /// <returns></returns>
    public GameObject SpawnProjectile(
        Transform spawnTransform,
        Collider2D parentCollider)
    {
        GameObject projectile = Instantiate(gameObject,
            spawnTransform.position, Quaternion.identity);
        projectile.transform.up = spawnTransform.up;

        Physics2D.IgnoreCollision(projectile.GetComponent<Collider2D>(),
            parentCollider);

        if (projectile.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = rb.transform.up * speed;
        }

        return projectile;
    }
}