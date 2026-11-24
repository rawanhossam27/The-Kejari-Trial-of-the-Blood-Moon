using UnityEngine;

/// <summary>
/// Keeps Serana in her imported stance while the Idle clip breathes through the spine and chest.
/// Walking, the magic attack, and death play on the whole body.
/// </summary>
public class SeranaBindBreathing : MonoBehaviour
{
    struct FrozenBone
    {
        public HumanBodyBones bone;
        public Quaternion localRotation;
    }

    Animator animator;
    FrozenBone[] frozen;

    void Start()
    {
        animator = GetComponent<Animator>();
        var kept = new System.Collections.Generic.List<FrozenBone>();
        for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
        {
            if (bone == HumanBodyBones.Spine || bone == HumanBodyBones.Chest || bone == HumanBodyBones.UpperChest)
                continue;
            var transform = animator.GetBoneTransform(bone);
            if (transform == null)
                continue;
            kept.Add(new FrozenBone { bone = bone, localRotation = transform.localRotation });
        }
        frozen = kept.ToArray();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (frozen == null || IsFullBodyClip())
            return;

        for (int i = 0; i < frozen.Length; i++)
            animator.SetBoneLocalRotation(frozen[i].bone, frozen[i].localRotation);
    }

    bool IsFullBodyClip()
    {
        if (IsOpen(animator.GetCurrentAnimatorStateInfo(0)))
            return true;
        return animator.IsInTransition(0) && IsOpen(animator.GetNextAnimatorStateInfo(0));
    }

    static bool IsOpen(AnimatorStateInfo info)
    {
        return info.IsName("Walking") || info.IsName("Attack") || info.IsName("Death");
    }
}
