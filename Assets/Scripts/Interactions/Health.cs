using System;
using UnityEngine;

namespace Pulsar.Interactions
{
public class Health : MonoBehaviour
{
    [field: SerializeField]
    [Min(1)]
    public float MaxHealth { get; set; }

    public float CurrentHealth
    {
        get => _currentHealth;
        private set => _currentHealth = Mathf.Clamp(value, NoHealth, MaxHealth);
    }

    private float _currentHealth;

    private const float NoHealth = 0;

    public event Action<Health> DestroyEvent;

    private void Awake()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        // Debug.Log("Hit. Current Health: " + HealthAmount);
        CurrentHealth -= damage;

        if (CurrentHealth <= NoHealth)
        {
            DestroyEvent?.Invoke(this);
        }
    }

    public void AddHealth(float health)
    {
        CurrentHealth += health;
    }
}
}