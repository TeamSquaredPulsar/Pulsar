using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [field: SerializeField]
    public float HealthAmount { get; private set; }

    private float NoHealth = 0;

    public void TakeDamage(float damage)
    {
        // Debug.Log("Hit. Current Health: " + HealthAmount);
        HealthAmount -= damage;

        if (HealthAmount <= NoHealth)
        {
            DestroyEvent?.Invoke(this);
        }
    }

    public void AddHealth(float health)
    {
        HealthAmount += health;
    }

    public event Action<Health> DestroyEvent;
}