using System;
using UnityEngine;

/// <summary>
/// Simple health pool. One swing should call TakeDamage once per target.
/// </summary>
public class Damageable : MonoBehaviour
{
    [SerializeField, Min(1f)] float maxHealth = 3f;
    [SerializeField, Min(0f)] float damageTakenScale = 1f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDefeated => CurrentHealth <= 0f;

    /// <summary>When false, hits are ignored. Used while the Alpha is dormant.</summary>
    public bool AcceptsDamage { get; set; } = true;

    public event Action Hit;
    public event Action Defeated;

    /// <summary>Current health, then maximum. Fired after a hit that changed health.</summary>
    public event Action<float, float> HealthChanged;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (!AcceptsDamage || IsDefeated || amount <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount * damageTakenScale);
        Hit?.Invoke();
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (IsDefeated)
            Defeated?.Invoke();
    }
}
