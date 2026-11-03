
using UnityEngine;

namespace Pulsar.Hazards
{
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
    public float Damage { get; protected set; } = 10f;

    [SerializeField]
    protected float damageRadius = 1f;

    [SerializeField]
    protected float speed = 10f;

    [SerializeField]
    [Range(0.1f, 10f)]
    private float lifeTime = 4;

    [SerializeField]
    private GameObject hitEffectPrefab;
    
    

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

        Destroy(gameObject);
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
            // Inherit the firing ship's motion once, when the bullet is launched.
            Rigidbody2D firingBody = parentCollider != null
                ? parentCollider.attachedRigidbody
                : null;
            Vector2 inheritedVelocity = firingBody != null
                ? firingBody.linearVelocity
                : Vector2.zero;
            rb.linearVelocity = inheritedVelocity + (Vector2)rb.transform.up * speed;
        }

        return projectile;
    }
}
}
