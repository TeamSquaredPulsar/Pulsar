using UnityEngine;

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
    private float lifeTime;

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
            // Hit Effect
            Debug.Log("Hit Effect");
        }

        // [AL] TODO: Will need to deal damage here?

        Destroy(gameObject);
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