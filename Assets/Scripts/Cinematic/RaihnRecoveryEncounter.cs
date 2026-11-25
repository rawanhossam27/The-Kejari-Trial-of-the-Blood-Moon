using System.Collections;
using UnityEngine;

/// <summary>
/// After Oraya is awake, looking at Raihn makes him walk to the bedside and offer a potion.
/// The look has to stay on his upper body. Being somewhere in the wide view is not enough.
/// </summary>
[DisallowMultipleComponent]
public class RaihnRecoveryEncounter : MonoBehaviour
{
    [SerializeField] Camera view;
    [SerializeField] BedRestLook look;
    [SerializeField] RecoveryAwakening awakening;
    [SerializeField] Transform raihn;
    [SerializeField] Transform aimPoint;
    [SerializeField] Transform[] approach;
    [SerializeField] DialogueData conversation;
    [SerializeField] HealingPotion potion;
    [SerializeField] float lookAngle = 12f;
    [SerializeField] float lookHold = 0.4f;
    [SerializeField] float walkSpeed = 1.2f;
    [SerializeField] float turnSpeed = 160f;

    bool spotted;
    bool walking;
    bool talking;
    bool healed;
    float held;
    int step;

    public bool HasSpotted => spotted;
    public bool IsWalking => walking;
    public bool HasSpoken => talking;

    void OnEnable()
    {
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.FlagChanged += HandleFlag;
    }

    void OnDisable()
    {
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.FlagChanged -= HandleFlag;
    }

    void Start()
    {
        if (!StoryStateManager.Exists)
            return;

        StoryStateManager story = StoryStateManager.Instance;
        if (story.IsFlagSet(StoryStateManager.Flags.RaihnPotionAccepted))
            healed = true;

        bool spoken = story.IsFlagSet(StoryStateManager.Flags.RaihnBedsideSpoken)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnPotionAccepted)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnPotionDeclined);
        if (spoken)
        {
            spotted = true;
            talking = true;
            SnapToBedside();
            return;
        }

        if (story.IsFlagSet(StoryStateManager.Flags.RaihnSpotted))
        {
            spotted = true;
            walking = true;
        }
    }

    void Update()
    {
        if (awakening != null && awakening.IsPlaying)
            return;
        if (look != null && !look.LookEnabled && !spotted)
            return;

        if (!spotted)
            Watch();
        else if (walking)
            Approach();
    }

    void Watch()
    {
        if (!IsLookingAtHim())
        {
            held = 0f;
            return;
        }

        held += Time.deltaTime;
        if (held < lookHold)
            return;

        spotted = true;
        walking = true;
        step = 0;
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.SetFlag(StoryStateManager.Flags.RaihnSpotted, true);
    }

    bool IsLookingAtHim()
    {
        if (view == null || aimPoint == null)
            return false;

        Vector3 to = aimPoint.position - view.transform.position;
        if (to.sqrMagnitude < 0.01f)
            return false;

        if (Vector3.Angle(view.transform.forward, to) > lookAngle)
            return false;

        Vector3 viewport = view.WorldToViewportPoint(aimPoint.position);
        if (viewport.z <= 0.05f || viewport.x < 0.02f || viewport.x > 0.98f || viewport.y < 0.02f || viewport.y > 0.98f)
            return false;

        Vector3 origin = view.transform.position;
        Vector3 direction = to.normalized;
        float distance = to.magnitude;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hit = hits[i].transform;
            if (hit == null)
                continue;
            if (raihn != null && (hit == raihn || hit.IsChildOf(raihn)))
                continue;
            if (hit.GetComponentInParent<OrayaMovement>() != null)
                continue;
            return false;
        }

        return true;
    }

    void Approach()
    {
        if (raihn == null || approach == null || step >= approach.Length)
        {
            Arrive();
            return;
        }

        Vector3 target = approach[step].position;
        target.y = raihn.position.y;
        Vector3 offset = target - raihn.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.006f)
        {
            step++;
            if (step >= approach.Length)
                Arrive();
            return;
        }

        Vector3 direction = offset.normalized;
        Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
        raihn.rotation = Quaternion.RotateTowards(raihn.rotation, facing, turnSpeed * Time.deltaTime);
        float distance = offset.magnitude;
        float stride = Mathf.Min(distance, walkSpeed * Time.deltaTime);
        raihn.position += direction * stride;
    }

    void Arrive()
    {
        walking = false;
        FaceOraya();
        if (talking || conversation == null)
            return;

        talking = true;
        StartCoroutine(SpeakWhenVisible());
    }

    IEnumerator SpeakWhenVisible()
    {
        if (look != null && raihn != null)
        {
            look.FocusOn(raihn.position + Vector3.up * 1.45f, 0.7f);
            float waited = 0f;
            while (look.IsFocusing && waited < 1.2f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null)
        {
            Debug.LogWarning("RaihnRecoveryEncounter: no DialogueManager in the Recovery Room.");
            yield break;
        }

        if (!dialogue.StartDialogue(conversation, false, 0f, false))
            Debug.LogWarning("RaihnRecoveryEncounter: the bedside conversation did not start.");
    }

    void SnapToBedside()
    {
        if (raihn == null || approach == null || approach.Length == 0 || approach[approach.Length - 1] == null)
            return;

        Vector3 target = approach[approach.Length - 1].position;
        target.y = raihn.position.y;
        raihn.position = target;
        FaceOraya();
    }

    void FaceOraya()
    {
        if (raihn == null || view == null)
            return;

        Vector3 to = view.transform.position - raihn.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f)
            return;

        raihn.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
    }

    void HandleFlag(string flagId, bool value)
    {
        if (!value || healed || potion == null)
            return;
        if (flagId != StoryStateManager.Flags.RaihnPotionAccepted)
            return;

        healed = true;
        potion.RecoverNow();
    }
}
