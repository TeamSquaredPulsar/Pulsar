using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private float speed = 10f;

    [SerializeField]
    private float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Fly in the direction the projectile is facing.
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // TODO: damage the player here
            Destroy(gameObject);
        }
    }
}