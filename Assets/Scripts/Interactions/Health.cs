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
    private HealthState _currentHealthState;


    private const float NoHealth = 0;
    private const float TenPercent = 0.1f;
    private const float TwentyFivePercent = 0.25f;
    private const float FiftyPercent = 0.5f;
    private const float SeventyFivePercent = 0.75f;

    public event Action<Health> DestroyEvent;
    public event Action<HealthState> HealthStateChanged;


    private void Awake()
    {
        CurrentHealth = MaxHealth;
        _currentHealthState = GetHealthState();
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        
        UpdateHealthState();

        if (CurrentHealth <= NoHealth)
        {
            DestroyEvent?.Invoke(this);
        }
    }

    public void AddHealth(float health)
    {
        CurrentHealth += health;
        
        UpdateHealthState();
    }
    
    private void UpdateHealthState()
    {
        HealthState newState = GetHealthState();

        if (newState == _currentHealthState)
        {
            return;
        }

        _currentHealthState = newState;

        HealthStateChanged?.Invoke(newState);
    }
    
    private HealthState GetHealthState()
    {
        float healthPerc = CurrentHealth / MaxHealth;

        return healthPerc switch
        {
            <= TenPercent => HealthState.LessOrEqualTo10Percent,
            <= TwentyFivePercent => HealthState.LessOrEqualTo25Percent,
            <= FiftyPercent => HealthState.LessOrEqualTo50Percent,
            <= SeventyFivePercent => HealthState.LessOrEqualTo75Percent,
            _ => HealthState.FullHealth
        };
    }
}
}