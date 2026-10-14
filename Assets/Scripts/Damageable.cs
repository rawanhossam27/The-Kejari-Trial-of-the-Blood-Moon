using System;
using UnityEngine;

/// <summary>
/// Simple health pool. One swing should call TakeDamage once per target.
/// </summary>
public class Damageable : MonoBehaviour
{
    [SerializeField, Min(1f)] float maxHealth = 3f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDefeated => CurrentHealth <= 0f;

    public event Action Hit;
    public event Action Defeated;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDefeated || amount <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Hit?.Invoke();
        if (IsDefeated)
            Defeated?.Invoke();
    }
}
