using System;
using UnityEngine;

/// <summary>
/// Represents a Projectile
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
/// 
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField]
    private float speed;

    [SerializeField]
    private float lifeTime;

    [SerializeField]
    private GameObject launchEffectPrefab;

    [SerializeField]
    private GameObject hitEffectPrefab;

    // Some useful setters
    public void SetSpeed(float input) => speed = input;

    /// <summary>
    /// Start method.
    /// </summary>
    private void Start()
    {
        transform.SetParent(null);
        // launch effect
        if (launchEffectPrefab != null)
        {
            Debug.Log("launch Effect");
        }
    }

    /// <summary>
    /// Move the projectile
    /// </summary>
    private void Update()
    {
        transform.position += transform.up * (speed * Time.deltaTime);
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
        if (collision.gameObject.GetComponent<Projectile>() != null)
        {
            return;
        }

        if (hitEffectPrefab != null)
        {
            // Hit Effect
            Debug.Log("Hit Effect");
        }

        // Will need to deal damage here?

        Destroy(gameObject);
    }
}