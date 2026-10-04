using UnityEngine;

public class Projectile : MonoBehaviour
{
    private const float MinAimSquared = 0.0001f;

    [SerializeField]
    private float speed = 10f;

    [SerializeField]
    private float lifetime = 5f;

    private Vector3 direction;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // TODO: damage the player here
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}