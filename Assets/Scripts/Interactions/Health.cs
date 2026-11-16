using System;
using NaughtyAttributes;
using UnityEngine;

namespace Pulsar.Interactions
{
public class Health : MonoBehaviour
{
    private const float NoHealth = 0;
    private const float TenPercent = 0.1f;
    private const float TwentyFivePercent = 0.25f;
    private const float FiftyPercent = 0.5f;
    private const float SeventyFivePercent = 0.75f;

    [field: SerializeField]
    [Min(1)]
    [field: OnValueChanged("SetCurrentHealth")]
    public float MaxHealth { get; set; }

    [field: ProgressBar("MaxHealth", EColor.Red)]
    [MaxValue("MaxHealth")]
    [SerializeField]
    private float currentHealth;

    public float CurrentHealth
    {
        get => currentHealth;
        private set => currentHealth = Mathf.Clamp(value, NoHealth, MaxHealth);
    }

    public HealthState CurrentHealthState { get; private set; }

    private void Awake()
    {
        SetCurrentHealth();
    }

    public event Action<Health> DestroyEvent;
    public event Action<HealthState> HealthStateChanged;

    private void SetCurrentHealth()
    {
        CurrentHealth = MaxHealth;
        CurrentHealthState = GetHealthState();
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

        if (newState == CurrentHealthState)
        {
            return;
        }

        CurrentHealthState = newState;

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
            _ => HealthState.FullHealth,
        };
    }
}
}
