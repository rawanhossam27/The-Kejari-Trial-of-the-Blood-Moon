using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One Moon Sword slash on left click. Hits are checked only while the swing's animation events say the blade is active.
/// </summary>
public class OrayaAttack : MonoBehaviour
{
    [SerializeField] OrayaMovement movement;
    [SerializeField] OrayaSwordGrip grip;
    [SerializeField] Animator animator;
    [SerializeField] Transform sword;
    [SerializeField, Min(0f)] float damage = 1f;
    [SerializeField, Min(0.1f)] float bladeLength = 1.55f;
    [SerializeField, Min(0.05f)] float hitRadius = 0.42f;
    [SerializeField, Min(0.1f)] float attackDuration = 2.5f;

    static readonly int AttackId = Animator.StringToHash("Attack");
    static readonly int SlashName = Animator.StringToHash("SwordSlash");

    readonly HashSet<Damageable> hitThisSwing = new HashSet<Damageable>();
    readonly Collider[] overlapBuffer = new Collider[16];
    bool attacking;
    bool hitWindow;
    float attackTimer;

    public bool IsAttacking => attacking;
    public bool HitWindowOpen => hitWindow;

    void Awake()
    {
        if (movement == null)
            movement = GetComponent<OrayaMovement>();
        if (grip == null)
            grip = GetComponent<OrayaSwordGrip>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (sword == null)
        {
            var found = transform.Find("root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_r/upperarm_r/lowerarm_r/hand_r/weapon_r/MoonSword");
            if (found != null)
                sword = found;
        }
    }

    void Update()
    {
        if (AttackPressed())
            TryAttack();

        if (!attacking)
            return;

        attackTimer += Time.deltaTime;
        if (hitWindow)
            ScanHits();

        bool playing = IsSlashPlaying();
        if ((!playing && attackTimer > 0.12f) || attackTimer >= attackDuration)
            FinishAttack();
    }

    public void TryAttack()
    {
        if (attacking || animator == null)
            return;
        if (movement != null && (!movement.InputEnabled || movement.CinematicMoveActive))
            return;

        animator.ResetTrigger(AttackId);
        animator.SetTrigger(AttackId);
        attacking = true;
        attackTimer = 0f;
        hitWindow = false;
        hitThisSwing.Clear();
        if (grip != null)
            grip.SuspendPoseLock = true;
    }

    /// <summary>Animation event on sword_attack_4. Opens the damage window.</summary>
    public void BeginSwordHit()
    {
        if (!attacking)
            return;

        hitWindow = true;
        hitThisSwing.Clear();
    }

    /// <summary>Animation event on sword_attack_4. Closes the damage window.</summary>
    public void EndSwordHit()
    {
        hitWindow = false;
    }

    void FinishAttack()
    {
        attacking = false;
        hitWindow = false;
        attackTimer = 0f;
        if (grip != null)
            grip.SuspendPoseLock = false;
        if (animator != null)
            animator.ResetTrigger(AttackId);
    }

    bool AttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
        return false;
#endif
    }

    bool IsSlashPlaying()
    {
        var current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash == SlashName)
            return true;
        if (!animator.IsInTransition(0))
            return false;
        return animator.GetNextAnimatorStateInfo(0).shortNameHash == SlashName;
    }

    void ScanHits()
    {
        if (sword == null)
            return;

        Vector3 hilt = sword.position;
        Vector3 tip = sword.TransformPoint(new Vector3(0f, bladeLength, 0f));
        int count = Physics.OverlapCapsuleNonAlloc(hilt, tip, hitRadius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var collider = overlapBuffer[i];
            if (collider == null || collider.transform.IsChildOf(transform))
                continue;

            var target = collider.GetComponentInParent<Damageable>();
            if (target == null || target.IsDefeated || !hitThisSwing.Add(target))
                continue;

            target.TakeDamage(damage);
        }
    }
}
