using UnityEngine;

/// <summary>
/// Opens the melee hit window from normalized time on an attack state.
/// Source clips are left untouched, so the contact timing lives on the state instead of an animation event.
/// </summary>
public class EnemyAttackWindow : StateMachineBehaviour
{
    [Range(0f, 1f)] public float openAt = 0.4f;
    [Range(0f, 1f)] public float closeAt = 0.62f;

    bool open;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        open = false;
        var combat = animator.GetComponent<EnemyCombatant>();
        if (combat != null)
            combat.NotifyAttackStarted();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var combat = animator.GetComponent<EnemyCombatant>();
        if (combat == null)
            return;

        float time = stateInfo.normalizedTime;
        if (time > 1f)
            time = 1f;

        if (!open && time >= openAt)
        {
            open = true;
            combat.OpenHitWindow();
        }

        if (open && time >= closeAt)
        {
            open = false;
            combat.CloseHitWindow();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var combat = animator.GetComponent<EnemyCombatant>();
        if (combat == null)
            return;

        combat.CloseHitWindow();
        combat.NotifyAttackFinished();
    }
}
