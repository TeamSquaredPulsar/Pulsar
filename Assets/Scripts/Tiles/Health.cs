using UnityEngine;

public class Health : MonoBehaviour
{
    [field: SerializeField]
    public float HealthAmount { get; private set; }

    private float NoHealth = 0;

    public void TakeDamage(float damage)
    {
        Debug.Log("Hit. Current Health: " + HealthAmount);
        HealthAmount -= damage;
    }

    public void AddHealth(float health)
    {
        HealthAmount += health;
    }

    private void Update()
    {
        if (HealthAmount <= NoHealth)
        {
            Destroy(gameObject);
        }
    }
}