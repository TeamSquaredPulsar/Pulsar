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

        Vector2 hitPoint = collision.GetContact(ContactPointIndex).point;
        Health health
            = GetColliderHealthComponent(collision.gameObject, hitPoint);

        if (health != null)
        {
            DealDamage(health);
        }

        Destroy(gameObject);
    }

    private void DealDamage(Health health)
    {
        health.TakeDamage(Damage);
    }

    private static Health GetColliderHealthComponent(
        GameObject collisionGameObject,
        Vector2 hitPoint)
    {
        Health colliderHealth = null;

        if (collisionGameObject.TryGetComponent(
                out ShipManager shipManager))
        {
            Debug.Log("COLLIDING SHIP MANAGER");
            List<Tile> tiles = shipManager.GetTiles();

            foreach (Tile tile in tiles)
            {
                Collider2D tileCollider = tile.GetComponent<Collider2D>();

                Vector2 closest = tileCollider.ClosestPoint(hitPoint);
                bool isCloseEnough = (closest - hitPoint).sqrMagnitude < 0.01f;

                if (isCloseEnough)
                {
                    colliderHealth = tile.GetComponent<Health>();
                    break;
                }
            }
        }
        else if (collisionGameObject.TryGetComponent(out Health health))
        {
            colliderHealth = health;
            Debug.Log("FOUND RAND COLLIDER");
        }

        return colliderHealth;
    }

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