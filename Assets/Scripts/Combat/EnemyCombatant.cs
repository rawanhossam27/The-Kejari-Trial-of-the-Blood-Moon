using System;
using UnityEngine;

/// <summary>
/// Shared melee enemy: aggro, chase, attack windows, hit reaction, and death.
/// Stats are per instance. Animation is delegated to <see cref="EnemyAnimationDriver"/>.
/// Damage taken uses the existing <see cref="Damageable"/> pool so the player's sword does not need a special case.
/// </summary>
public class EnemyCombatant : MonoBehaviour
{
    [SerializeField] Damageable health;
    [SerializeField] EnemyAnimationDriver animationDriver;
    [SerializeField] Transform hitOrigin;
    [SerializeField, Min(0.5f)] float aggroRadius = 12f;
    [SerializeField, Min(0.5f)] float attackRadius = 4.4f;
    [SerializeField, Min(0.5f)] float runDistance = 7f;
    [SerializeField, Min(0f)] float moveSpeed = 2.6f;
    [SerializeField, Min(0f)] float runSpeed = 4.8f;
    [SerializeField, Min(0f)] float turnSpeed = 240f;
    [SerializeField, Min(0f)] float attackDamage = 18f;
    [SerializeField, Min(0f)] float attackCooldown = 1.35f;
    [SerializeField, Min(0.1f)] float hitRadius = 1.45f;
    [SerializeField, Min(1)] int attackCount = 3;
    [SerializeField, Min(0f)] float hitStun = 0.35f;
    [SerializeField] float aimHeight = 1.1f;
    [SerializeField] bool dormant = true;
    [SerializeField, Min(0f)] float awakenGrace = 1.7f;
    [SerializeField, Min(0f)] float approachBeforeBite = 1.1f;

    readonly Collider[] overlap = new Collider[16];

    OrayaMovement player;
    float spawnHeight;
    float nextAttackTime;
    float stunUntil;
    int nextAttack = 1;
    bool aggro;
    bool attacking;
    bool windowOpen;
    bool hitThisSwing;
    bool dead;
    bool roused;
    float wakeUntil;

    public bool IsAggro => aggro;
    public bool IsAttacking => attacking;
    public bool IsDead => dead;
    public bool IsDormant => dormant;
    public bool IsWaking => !dormant && Time.time < wakeUntil;
    public float AttackRadius => attackRadius;
    public event Action AggroStarted;

    void Awake()
    {
        if (health == null)
            health = GetComponent<Damageable>();
        if (animationDriver == null)
            animationDriver = GetComponent<EnemyAnimationDriver>();
        if (hitOrigin == null)
        {
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "Bip01 Head")
                {
                    hitOrigin = bone;
                    break;
                }
            }
        }

        player = FindAnyObjectByType<OrayaMovement>();
        spawnHeight = transform.position.y;
    }

    void Start()
    {
        if (dormant)
            ApplyDormant();
    }

    /// <summary>
    /// Ends the dormant state. The demon turns toward Oraya before it is allowed to chase or bite.
    /// </summary>
    public void Awaken()
    {
        if (dead || !dormant)
            return;

        dormant = false;
        wakeUntil = Time.time + awakenGrace;
        nextAttackTime = wakeUntil + approachBeforeBite;
        if (health != null)
            health.AcceptsDamage = true;

        foreach (var collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = true;

        var bar = transform.Find("EnemyHealthBar");
        if (bar != null)
            bar.gameObject.SetActive(true);
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.Hit += OnHit;
            health.Defeated += OnDefeated;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.Hit -= OnHit;
            health.Defeated -= OnDefeated;
        }
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (dead || (health != null && health.IsDefeated))
            return;

        if (dormant)
        {
            SetMove(0f, false);
            return;
        }

        if (Time.time < wakeUntil)
        {
            if (player != null && CanEngage())
                Face(player.transform.position, deltaTime, 0.4f);
            SetMove(0f, true);
            return;
        }

        if (!roused)
        {
            roused = true;
            aggro = true;
            AggroStarted?.Invoke();
        }

        if (!CanEngage())
        {
            SetMove(0f, aggro);
            return;
        }

        Vector3 target = player.transform.position;
        float distance = FlatDistance(target);
        if (!aggro)
        {
            if (distance <= aggroRadius && PathClear(ChestPosition(), target + Vector3.up * aimHeight))
                BeginAggro();
            else
            {
                SetMove(0f, false);
                return;
            }
        }

        Face(target, deltaTime, 1f);

        if (Time.time < stunUntil)
        {
            SetMove(0f, true);
            return;
        }

        if (attacking)
        {
            SetMove(0f, true);
            if (windowOpen)
                TryStrike();
            return;
        }

        if (distance <= attackRadius)
        {
            SetMove(0f, true);
            if (Time.time >= nextAttackTime)
                BeginAttack();
            return;
        }

        float speed = distance > runDistance ? runSpeed : moveSpeed;
        if (!BlockedAhead(speed * deltaTime))
        {
            Vector3 next = transform.position + transform.forward * (speed * deltaTime);
            next.y = spawnHeight;
            transform.position = next;
        }

        SetMove(distance > runDistance ? 2f : 1f, true);
    }

    public void NotifyAttackStarted()
    {
        if (dead || dormant)
            return;

        attacking = true;
        hitThisSwing = false;
        windowOpen = false;
    }

    public void NotifyAttackFinished()
    {
        windowOpen = false;
        if (!attacking)
            return;

        attacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    public void OpenHitWindow()
    {
        if (dead || dormant || Time.time < wakeUntil)
            return;

        windowOpen = true;
        TryStrike();
    }

    public void CloseHitWindow()
    {
        windowOpen = false;
    }

    void BeginAggro()
    {
        aggro = true;
        AggroStarted?.Invoke();
    }

    void BeginAttack()
    {
        attacking = true;
        hitThisSwing = false;
        windowOpen = false;
        int index = nextAttack;
        nextAttack = nextAttack % Mathf.Max(1, attackCount) + 1;
        if (animationDriver != null)
            animationDriver.PlayAttack(index);
    }

    void TryStrike()
    {
        if (hitThisSwing || player == null || dead)
            return;

        var playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead)
            return;

        Vector3 origin = hitOrigin != null ? hitOrigin.position : ChestPosition();
        Vector3 aim = player.transform.position + Vector3.up * aimHeight;
        Vector3 toPlayer = aim - origin;
        if (toPlayer.sqrMagnitude > (hitRadius + 0.6f) * (hitRadius + 0.6f))
            return;

        Vector3 flat = toPlayer;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f && Vector3.Dot(transform.forward, flat.normalized) < 0.15f)
            return;

        if (!PathClear(origin, aim))
            return;

        int count = Physics.OverlapSphereNonAlloc(origin, hitRadius, overlap, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlap[i];
            if (collider == null || collider.transform.IsChildOf(transform))
                continue;

            var hurt = collider.GetComponentInParent<PlayerHealth>();
            if (hurt == null || hurt.IsDead)
                continue;

            hitThisSwing = true;
            hurt.TakeDamage(attackDamage);
            return;
        }
    }

    void OnHit()
    {
        if (dead || dormant || attacking || animationDriver == null)
            return;

        stunUntil = Time.time + hitStun;
        animationDriver.PlayHit();
    }

    void OnDefeated()
    {
        if (dead)
            return;

        dead = true;
        attacking = false;
        windowOpen = false;
        aggro = false;
        if (animationDriver != null)
            animationDriver.PlayDeath();

        foreach (var collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;
    }

    bool CanEngage()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<OrayaMovement>();
            if (player == null)
                return false;
        }

        if (!player.InputEnabled || player.CinematicMoveActive)
            return false;

        var playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null && playerHealth.IsDead)
            return false;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning)
            return false;

        return true;
    }

    void Face(Vector3 worldPoint, float deltaTime, float turnScale)
    {
        Vector3 flat = worldPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;

        Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
        float step = turnSpeed * Mathf.Clamp01(turnScale) * deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, step);
    }

    void ApplyDormant()
    {
        if (health != null)
            health.AcceptsDamage = false;

        foreach (var collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        var bar = transform.Find("EnemyHealthBar");
        if (bar != null)
            bar.gameObject.SetActive(false);
    }

    void SetMove(float speed, bool inCombat)
    {
        if (animationDriver != null)
            animationDriver.SetLocomotion(speed, inCombat, 0.12f);
    }

    float FlatDistance(Vector3 worldPoint)
    {
        Vector3 delta = worldPoint - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    Vector3 ChestPosition()
    {
        return transform.position + Vector3.up * 1.2f;
    }

    bool BlockedAhead(float distance)
    {
        Vector3 origin = ChestPosition();
        if (!Physics.SphereCast(origin, 0.45f, transform.forward, out RaycastHit hit, distance + 0.35f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.transform.IsChildOf(transform))
            return false;

        return hit.collider.GetComponentInParent<PlayerHealth>() == null;
    }

    bool PathClear(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.05f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(from, delta / distance, distance, ~0, QueryTriggerInteraction.Ignore);
        float nearestWall = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform.IsChildOf(transform))
                continue;

            if (hitTransform.GetComponentInParent<PlayerHealth>() != null)
                continue;

            if (hits[i].distance < nearestWall)
                nearestWall = hits[i].distance;
        }

        return nearestWall >= distance - 0.05f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.2f, 0.15f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = new Color(0.9f, 0.55f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
