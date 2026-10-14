using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Oraya's health. The training-target <see cref="Damageable"/> pool is separate and is not used here.
/// </summary>
[DefaultExecutionOrder(-50)]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] float maxHealth = 100f;
    [SerializeField] OrayaMovement movement;
    [SerializeField] OrayaSwordGrip grip;
    [SerializeField] Animator animator;
#if UNITY_EDITOR
    [SerializeField] bool debugHotkeys;
#endif

    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int AttackId = Animator.StringToHash("Attack");
    static readonly int DieId = Animator.StringToHash("Die");
    static readonly int DeadId = Animator.StringToHash("Dead");

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    /// <summary>Current health, then max health. Fired after damage and healing.</summary>
    public event Action<float, float> HealthChanged;

    public event Action Died;

    void Awake()
    {
        if (movement == null)
            movement = GetComponent<OrayaMovement>();
        if (grip == null)
            grip = GetComponent<OrayaSwordGrip>();
        if (animator == null)
            animator = GetComponent<Animator>();

        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (IsDead)
            LockControls();

#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
        if (!debugHotkeys || IsDead)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (keyboard.leftBracketKey.wasPressedThisFrame)
            TakeDamage(10f);
        if (keyboard.rightBracketKey.wasPressedThisFrame)
            Heal(10f);
#endif
    }

    void LateUpdate()
    {
        if (!IsDead || grip == null)
            return;

        // Runs before OrayaSwordGrip so the death pose is not overwritten by the idle sword lock.
        grip.SuspendPoseLock = true;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        float healed = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        if (Mathf.Approximately(healed, CurrentHealth))
            return;

        CurrentHealth = healed;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        LockControls();
        if (grip != null)
            grip.SuspendPoseLock = true;

        if (animator != null && HasParameter(DieId))
        {
            animator.ResetTrigger(AttackId);
            animator.SetTrigger(DieId);
            if (HasParameter(DeadId))
                animator.SetBool(DeadId, true);
        }
        else
        {
            Debug.LogWarning("PlayerHealth: no Die parameter on Oraya's Animator. Movement and attacks are disabled, and sword_death was not played.");
        }

        Died?.Invoke();
    }

    void LockControls()
    {
        if (movement != null)
            movement.InputEnabled = false;
        if (animator != null)
            animator.SetFloat(SpeedId, 0f);
    }

    bool HasParameter(int hash)
    {
        if (animator == null)
            return false;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == hash)
                return true;
        }

        return false;
    }

    [ContextMenu("Debug/Take 10 Damage")]
    void DebugTakeDamage()
    {
        if (Application.isPlaying)
            TakeDamage(10f);
    }

    [ContextMenu("Debug/Heal 10")]
    void DebugHeal()
    {
        if (Application.isPlaying)
            Heal(10f);
    }

    [ContextMenu("Debug/Lethal Damage")]
    void DebugLethal()
    {
        if (Application.isPlaying)
            TakeDamage(CurrentHealth);
    }
}
