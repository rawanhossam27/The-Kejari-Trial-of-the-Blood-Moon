using System;
using UnityEngine;

/// <summary>
/// Steps Oraya up to the exit lever and reaches for the handle with her free hand.
/// The sword arm is left on its idle grip. The pull starts once her hand arrives.
/// </summary>
[DefaultExecutionOrder(300)]
public class OrayaLeverReach : MonoBehaviour
{
    const string UpperPath = "root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_l/upperarm_l";
    const float ComfortReach = 0.46f;
    const float ApproachSpeed = 2.6f;

    Transform upper;
    Transform lower;
    Transform hand;
    Transform handle;
    CharacterController body;
    OrayaMovement movement;
    OrayaDaggerThrow dagger;
    PlayerHealth health;

    Vector3 upperAxisLocal;
    Vector3 lowerAxisLocal;
    Vector3 fingerAxisLocal;
    Vector3 grabLocal;
    Action onGrab;

    float weight;
    float holdTime;
    float elapsed;
    bool playing;
    bool pulled;
    bool restoreInput;

    public bool IsReaching => playing;

    public void Begin(Transform leverHandle, Action grabbed)
    {
        if (playing)
            return;

        Cache();
        if (upper == null || lower == null || hand == null || leverHandle == null)
        {
            grabbed?.Invoke();
            return;
        }

        if (dagger != null && dagger.IsBusy)
        {
            grabbed?.Invoke();
            return;
        }

        handle = leverHandle;
        onGrab = grabbed;
        var renderer = leverHandle.GetComponentInChildren<Renderer>();
        grabLocal = renderer != null
            ? leverHandle.InverseTransformPoint(renderer.bounds.center)
            : new Vector3(0f, 0f, 0.12f);

        upperAxisLocal = AxisLocal(upper, lower);
        lowerAxisLocal = AxisLocal(lower, hand);
        var finger = hand.Find("index_metacarpal_l/index_01_l");
        fingerAxisLocal = finger != null
            ? hand.InverseTransformDirection(finger.position - hand.position).normalized
            : Vector3.left;

        if (dagger != null)
            dagger.SetCarriedDaggerVisible(false);

        playing = true;
        pulled = false;
        weight = 0f;
        holdTime = 0f;
        elapsed = 0f;
        if (movement != null && movement.InputEnabled && !movement.CinematicMoveActive)
        {
            movement.InputEnabled = false;
            restoreInput = true;
        }
    }

    void Awake()
    {
        body = GetComponent<CharacterController>();
        movement = GetComponent<OrayaMovement>();
        dagger = GetComponent<OrayaDaggerThrow>();
        health = GetComponent<PlayerHealth>();
        Cache();
    }

    void OnDisable()
    {
        if (!playing)
            return;
        playing = false;
        RestoreInput();
    }

    void Cache()
    {
        if (upper != null)
            return;
        upper = transform.Find(UpperPath);
        lower = upper != null ? upper.Find("lowerarm_l") : null;
        hand = lower != null ? lower.Find("hand_l") : null;
    }

    void Update()
    {
        if (!playing || health != null && health.IsDead)
        {
            if (playing)
                Finish();
            return;
        }

        elapsed += Time.deltaTime;
        Vector3 grab = handle.TransformPoint(grabLocal);
        Vector3 shoulder = upper.position;
        Vector3 toGrab = grab - shoulder;
        float distance = toGrab.magnitude;

        Vector3 face = grab - transform.position;
        face.y = 0f;
        if (face.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 520f * Time.deltaTime);
        }

        if (!pulled && distance > ComfortReach && body != null)
        {
            Vector3 step = toGrab / distance;
            float room = distance - ComfortReach;
            body.Move(new Vector3(step.x, 0f, step.z) * Mathf.Min(ApproachSpeed * Time.deltaTime, room));
        }

        bool arrived = distance <= ComfortReach + 0.08f;
        if (!pulled && ((arrived && weight > 0.72f) || elapsed > 0.9f))
        {
            pulled = true;
            holdTime = 0f;
            onGrab?.Invoke();
            onGrab = null;
        }

        if (pulled)
            holdTime += Time.deltaTime;

        float desired = pulled && holdTime > 0.45f ? 0f : (arrived || elapsed > 0.35f ? 1f : Mathf.Clamp01(elapsed / 0.35f));
        weight = Mathf.MoveTowards(weight, desired, Time.deltaTime / 0.22f);

        if (pulled && holdTime > 0.85f && weight <= 0.02f)
            Finish();
        else if (elapsed > 2.5f)
        {
            Finish();
        }
    }

    void LateUpdate()
    {
        if (!playing || weight <= 0.001f || handle == null || upper == null)
            return;

        Vector3 grab = handle.TransformPoint(grabLocal);
        var finger = hand.Find("index_metacarpal_l/index_01_l");
        if (finger != null)
            fingerAxisLocal = hand.InverseTransformDirection(finger.position - hand.position).normalized;
        Quaternion savedUpper = upper.rotation;
        Quaternion savedLower = lower.rotation;
        Quaternion savedHand = hand.rotation;

        SolveArm(grab);

        upper.rotation = Quaternion.Slerp(savedUpper, upper.rotation, weight);
        lower.rotation = Quaternion.Slerp(savedLower, lower.rotation, weight);
        AimHand(grab);
        hand.rotation = Quaternion.Slerp(savedHand, hand.rotation, weight);
    }

    void SolveArm(Vector3 target)
    {
        float upperLen = Vector3.Distance(upper.position, lower.position);
        float lowerLen = Vector3.Distance(lower.position, hand.position);
        if (upperLen < 0.01f || lowerLen < 0.01f)
            return;

        Vector3 rootPos = upper.position;
        Vector3 toTarget = target - rootPos;
        float dist = toTarget.magnitude;
        float maxReach = upperLen + lowerLen - 0.01f;
        float minReach = Mathf.Abs(upperLen - lowerLen) + 0.01f;
        dist = Mathf.Clamp(dist, minReach, maxReach);
        Vector3 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : transform.forward;

        float cos = (upperLen * upperLen + dist * dist - lowerLen * lowerLen) / (2f * upperLen * dist);
        float bend = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
        Vector3 pole = -transform.up + -transform.right * 0.65f;
        Vector3 axis = Vector3.Cross(dir, pole);
        if (axis.sqrMagnitude < 0.0001f)
            axis = transform.forward;
        axis.Normalize();

        Vector3 upperDir = Quaternion.AngleAxis(bend, axis) * dir;
        AlignBone(upper, upperAxisLocal, upperDir);
        Vector3 lowerDir = target - lower.position;
        if (lowerDir.sqrMagnitude < 0.0001f)
            lowerDir = upperDir;
        AlignBone(lower, lowerAxisLocal, lowerDir);
    }

    void AimHand(Vector3 target)
    {
        Vector3 along = target - hand.position;
        if (along.sqrMagnitude < 0.0001f)
            along = lower.TransformDirection(lowerAxisLocal);
        Vector3 current = hand.TransformDirection(fingerAxisLocal);
        if (current.sqrMagnitude < 0.0001f)
            return;
        hand.rotation = Quaternion.FromToRotation(current, along.normalized) * hand.rotation;
    }

    static void AlignBone(Transform bone, Vector3 axisLocal, Vector3 worldDir)
    {
        Vector3 current = bone.TransformDirection(axisLocal);
        if (current.sqrMagnitude < 0.0001f || worldDir.sqrMagnitude < 0.0001f)
            return;
        bone.rotation = Quaternion.FromToRotation(current, worldDir.normalized) * bone.rotation;
    }

    static Vector3 AxisLocal(Transform bone, Transform child)
    {
        return bone.InverseTransformDirection(child.position - bone.position).normalized;
    }

    void Finish()
    {
        if (!pulled)
        {
            pulled = true;
            onGrab?.Invoke();
            onGrab = null;
        }

        playing = false;
        weight = 0f;
        if (dagger != null)
            dagger.SetCarriedDaggerVisible(true);
        RestoreInput();
    }

    void RestoreInput()
    {
        if (!restoreInput || movement == null)
            return;
        restoreInput = false;
        if (health != null && health.IsDead)
            return;
        if (!movement.CinematicMoveActive)
            movement.InputEnabled = true;
    }
}
