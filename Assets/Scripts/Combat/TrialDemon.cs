using UnityEngine;

public enum TrialDemonRole
{
    Normal = 0,
    Alpha = 1
}

/// <summary>
/// Marks a demon as part of the Blood Moon trial.
/// A normal death stays local. The Alpha's death asks <see cref="BloodMoonTrial"/> to end the trial.
/// </summary>
[DisallowMultipleComponent]
public class TrialDemon : MonoBehaviour
{
    [SerializeField] TrialDemonRole role = TrialDemonRole.Normal;
    [SerializeField] Damageable health;
    [SerializeField] EnemyCombatant combat;

    bool deathHandled;

    public TrialDemonRole Role => role;

    void Awake()
    {
        if (health == null)
            health = GetComponent<Damageable>();
        if (combat == null)
            combat = GetComponent<EnemyCombatant>();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        BloodMoonTrial.Ensure().Register(this);
        if (health != null)
            health.Defeated += HandleDeath;
        if (role == TrialDemonRole.Alpha && combat != null)
            combat.AggroStarted += HandleAggro;
    }

    void OnDisable()
    {
        if (BloodMoonTrial.Instance != null)
            BloodMoonTrial.Instance.Unregister(this);
        if (health != null)
            health.Defeated -= HandleDeath;
        if (combat != null)
            combat.AggroStarted -= HandleAggro;
    }

    /// <summary>Kills this demon because the Alpha fell. Does nothing to the Alpha.</summary>
    public void DefeatFromTrial()
    {
        if (role == TrialDemonRole.Alpha || health == null || health.IsDefeated)
            return;

        health.TakeDamage(1000000f);
    }

    void HandleAggro()
    {
        if (!StoryStateManager.Exists)
            return;

        StoryStateManager.Instance.SetFlag(StoryStateManager.Flags.AlphaIdentified, true);
    }

    void HandleDeath()
    {
        if (deathHandled)
            return;

        deathHandled = true;
        if (role != TrialDemonRole.Alpha)
            return;

        BloodMoonTrial trial = BloodMoonTrial.Instance != null ? BloodMoonTrial.Instance : BloodMoonTrial.Ensure();
        trial.ResolveAlphaDeath(this);
    }
}
