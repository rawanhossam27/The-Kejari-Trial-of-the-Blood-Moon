using UnityEngine;

/// <summary>
/// Stops Serana once her <see cref="Damageable"/> health reaches zero and plays her death clip.
/// </summary>
public class SeranaDeath : MonoBehaviour
{
    [SerializeField] Damageable health;
    [SerializeField] SeranaMagicAttack attack;
    [SerializeField] EnemyProximityDialogue encounter;
    [SerializeField] Animator animator;

    bool dead;

    public bool IsDead => dead;

    void Awake()
    {
        if (health == null)
            health = GetComponent<Damageable>();
        if (attack == null)
            attack = GetComponent<SeranaMagicAttack>();
        if (encounter == null)
            encounter = GetComponent<EnemyProximityDialogue>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (health != null)
            health.Defeated += OnDefeated;
    }

    void OnDisable()
    {
        if (health != null)
            health.Defeated -= OnDefeated;
    }

    void OnDefeated()
    {
        if (dead)
            return;

        dead = true;
        if (attack != null)
            attack.StopCombat();
        MagicProjectile.DisarmFrom(transform);
        if (encounter != null)
            encounter.enabled = false;

        foreach (var collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;

        PlayDeath();
    }

    void PlayDeath()
    {
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.ResetTrigger("Attack");
        animator.Play("Death", 0, 0f);
    }
}
