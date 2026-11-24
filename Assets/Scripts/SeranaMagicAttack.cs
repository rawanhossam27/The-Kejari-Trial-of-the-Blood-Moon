using UnityEngine;

/// <summary>
/// Plays Serana's cast and releases one non-homing bolt toward Oraya.
/// Stays in place. The next cast waits for this one to finish and for the cooldown.
/// </summary>
public class SeranaMagicAttack : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] EnemyProximityDialogue encounter;
    [SerializeField] MagicProjectile projectilePrefab;
    [SerializeField] Transform player;
    [SerializeField] OrayaMovement movement;
    [SerializeField, Min(0.5f)] float cooldown = 5f;
    [SerializeField, Min(0.05f)] float releaseTime = 1.35f;
    [SerializeField, Min(0.1f)] float projectileSpeed = 10f;
    [SerializeField, Min(0f)] float damage = 15f;
    [SerializeField, Min(0.5f)] float projectileLifetime = 4f;
    [SerializeField] float aimHeight = 1f;
    [SerializeField] float spawnForward = 0.2f;

    bool casting;
    bool entered;
    bool released;
    bool stopped;
    float castStartedAt;
    float nextAttackTime;

    public bool IsCasting => casting;
    public bool HasReleased => released;
    public bool IsStopped => stopped;

    public void StopCombat()
    {
        stopped = true;
        casting = false;
        if (animator != null)
            animator.ResetTrigger("Attack");
        enabled = false;
    }

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (encounter == null)
            encounter = GetComponent<EnemyProximityDialogue>();
        if (movement == null)
            movement = UnityEngine.Object.FindAnyObjectByType<OrayaMovement>();
        if (player == null && movement != null)
            player = movement.transform;
    }

    void Update()
    {
        if (stopped)
            return;

        if (casting)
        {
            TickCast();
            return;
        }

        if (Time.time < nextAttackTime || !CanAttack())
            return;

        BeginAttack();
    }

    void BeginAttack()
    {
        casting = true;
        entered = false;
        released = false;
        castStartedAt = Time.time;
        FacePlayer();
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    void TickCast()
    {
        if (IsAttackState())
        {
            if (!entered)
            {
                entered = true;
                nextAttackTime = Time.time + cooldown;
            }

            TryRelease();
            return;
        }

        if (entered)
        {
            casting = false;
            return;
        }

        if (Time.time > castStartedAt + 0.5f)
            casting = false;
    }

    void TryRelease()
    {
        if (released)
            return;

        if (animator.IsInTransition(0) && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            return;

        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("Attack") || info.normalizedTime * info.length < releaseTime)
            return;

        released = true;
        Launch();
    }

    void Launch()
    {
        if (projectilePrefab == null || player == null)
            return;

        Vector3 origin = SpawnPoint();
        Vector3 target = player.position + Vector3.up * aimHeight;
        Vector3 direction = target - origin;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        var bolt = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction.normalized, Vector3.up));
        bolt.Launch(direction, projectileSpeed, damage, projectileLifetime, transform);
    }

    Vector3 SpawnPoint()
    {
        Transform right = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        Transform left = animator != null ? animator.GetBoneTransform(HumanBodyBones.LeftHand) : null;
        Vector3 origin;
        if (right != null && left != null)
            origin = (right.position + left.position) * 0.5f;
        else if (right != null)
            origin = right.position;
        else if (left != null)
            origin = left.position;
        else
            origin = transform.position + Vector3.up * aimHeight;

        return origin + transform.forward * spawnForward;
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
    }

    bool CanAttack()
    {
        if (stopped || animator == null || projectilePrefab == null || player == null)
            return false;

        var self = GetComponent<Damageable>();
        if (self != null && self.IsDefeated)
            return false;

        if (encounter != null && !encounter.HasPlayed)
            return false;

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue != null && dialogue.IsRunning)
            return false;

        if (movement != null && (!movement.InputEnabled || movement.CinematicMoveActive))
            return false;

        var health = player.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead)
            return false;

        return true;
    }

    bool IsAttackState()
    {
        if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Attack"))
            return true;

        return animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
    }
}
