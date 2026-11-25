using UnityEngine;

/// <summary>
/// Drives a Generic enemy animator. Combat code never names clips.
/// The controller is expected to use Speed, Combat, Attack, AttackIndex, Hit, Die, and Dead.
/// Missing parameters are ignored so a simpler enemy can share the same combat script.
/// </summary>
public class EnemyAnimationDriver : MonoBehaviour
{
    [SerializeField] Animator animator;

    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int CombatId = Animator.StringToHash("Combat");
    static readonly int AttackId = Animator.StringToHash("Attack");
    static readonly int AttackIndexId = Animator.StringToHash("AttackIndex");
    static readonly int HitId = Animator.StringToHash("Hit");
    static readonly int DieId = Animator.StringToHash("Die");
    static readonly int DeadId = Animator.StringToHash("Dead");

    bool parametersCached;
    bool hasSpeed;
    bool hasCombat;
    bool hasAttack;
    bool hasAttackIndex;
    bool hasHit;
    bool hasDie;
    bool hasDead;

    public bool IsDead => animator != null && hasDead && animator.GetBool(DeadId);

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        CacheParameters();
    }

    public void SetLocomotion(float speed, bool inCombat, float damp)
    {
        CacheParameters();
        if (animator == null)
            return;

        if (hasSpeed)
            animator.SetFloat(SpeedId, speed, Mathf.Max(0f, damp), Time.deltaTime);
        if (hasCombat)
            animator.SetBool(CombatId, inCombat);
    }

    public void PlayAttack(int index)
    {
        CacheParameters();
        if (animator == null || !hasAttack)
            return;

        if (hasAttackIndex)
            animator.SetInteger(AttackIndexId, index);
        animator.ResetTrigger(AttackId);
        animator.SetTrigger(AttackId);
    }

    public void PlayHit()
    {
        CacheParameters();
        if (animator == null || !hasHit || IsDead)
            return;

        animator.ResetTrigger(HitId);
        animator.SetTrigger(HitId);
    }

    public void PlayDeath()
    {
        CacheParameters();
        if (animator == null)
            return;

        if (hasAttack)
            animator.ResetTrigger(AttackId);
        if (hasHit)
            animator.ResetTrigger(HitId);
        if (hasDie)
        {
            animator.ResetTrigger(DieId);
            animator.SetTrigger(DieId);
        }
        if (hasDead)
            animator.SetBool(DeadId, true);
    }

    public bool InAttack
    {
        get
        {
            if (animator == null)
                return false;

            if (NameIsAttack(animator.GetCurrentAnimatorStateInfo(0)))
                return true;
            return animator.IsInTransition(0) && NameIsAttack(animator.GetNextAnimatorStateInfo(0));
        }
    }

    static bool NameIsAttack(AnimatorStateInfo info)
    {
        return info.IsName("Attack1") || info.IsName("Attack2") || info.IsName("Attack3");
    }

    void CacheParameters()
    {
        if (parametersCached || animator == null)
            return;

        parametersCached = true;
        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == SpeedId) hasSpeed = true;
            else if (parameter.nameHash == CombatId) hasCombat = true;
            else if (parameter.nameHash == AttackId) hasAttack = true;
            else if (parameter.nameHash == AttackIndexId) hasAttackIndex = true;
            else if (parameter.nameHash == HitId) hasHit = true;
            else if (parameter.nameHash == DieId) hasDie = true;
            else if (parameter.nameHash == DeadId) hasDead = true;
        }
    }
}
