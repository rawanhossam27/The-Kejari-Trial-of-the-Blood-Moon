using UnityEngine;

/// <summary>
/// Turns the spine, neck, and head toward Oraya. The character root is never rotated.
/// Runs after <see cref="NPCBreathing"/> and rebuilds those bones from their bind rotations.
/// </summary>
[DefaultExecutionOrder(20)]
public class NPCLookAtPlayer : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] OrayaMovement movement;
    [SerializeField] string spineBoneName = "Spine";
    [SerializeField] string neckBoneName = "Neck";
    [SerializeField] string headBoneName = "Head";
    [SerializeField, Min(0.2f)] float radius = 5f;
    [SerializeField, Min(1f)] float turnSpeed = 70f;
    [SerializeField, Min(1f)] float returnSpeed = 70f;
    [SerializeField] float spineYawLimit = 12f;
    [SerializeField] float neckYawLimit = 30f;
    [SerializeField] float headYawLimit = 35f;
    [SerializeField] float spinePitchLimit = 4f;
    [SerializeField] float neckPitchLimit = 8f;
    [SerializeField] float headPitchLimit = 12f;

    PoseBone spine;
    PoseBone neck;
    PoseBone head;
    NPCBreathing breathing;
    Transform playerHead;
    float smoothedYaw;
    float smoothedPitch;

    /// <summary>True while Oraya is inside <see cref="radius"/>.</summary>
    public bool IsTracking { get; private set; }

    void Awake()
    {
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
        if (player == null && movement != null)
            player = movement.transform;
        breathing = GetComponent<NPCBreathing>();
        spine = PoseBone.Capture(transform, spineBoneName);
        neck = PoseBone.Capture(transform, neckBoneName);
        head = PoseBone.Capture(transform, headBoneName);
        if (player != null)
        {
            foreach (Transform bone in player.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name != "head")
                    continue;
                playerHead = bone;
                break;
            }
        }
    }

    void LateUpdate()
    {
        Vector3 aimPoint = AimPoint();
        bool tracking = player != null && head.found && WithinRadius(aimPoint);
        IsTracking = tracking;

        float desiredYaw = 0f;
        float desiredPitch = 0f;
        if (tracking)
            MeasureLook(aimPoint, out desiredYaw, out desiredPitch);

        float yawSpeed = tracking ? turnSpeed : returnSpeed;
        float pitchSpeed = tracking ? turnSpeed : returnSpeed;
        smoothedYaw = Mathf.MoveTowardsAngle(smoothedYaw, desiredYaw, yawSpeed * Time.deltaTime);
        smoothedPitch = Mathf.MoveTowards(smoothedPitch, desiredPitch, pitchSpeed * Time.deltaTime);

        float yawSum = Mathf.Max(0.01f, spineYawLimit + neckYawLimit + headYawLimit);
        float pitchSum = Mathf.Max(0.01f, spinePitchLimit + neckPitchLimit + headPitchLimit);
        float spineYaw = smoothedYaw * (spineYawLimit / yawSum);
        float neckYaw = smoothedYaw * (neckYawLimit / yawSum);
        float headYaw = smoothedYaw * (headYawLimit / yawSum);
        float spinePitch = smoothedPitch * (spinePitchLimit / pitchSum);
        float neckPitch = smoothedPitch * (neckPitchLimit / pitchSum);
        float headPitch = smoothedPitch * (headPitchLimit / pitchSum);

        float breathSpine = breathing != null ? breathing.SpinePitch : 0f;
        float breathChest = breathing != null ? breathing.ChestPitch : 0f;

        spine.Write(spineYaw, breathSpine + spinePitch, transform);
        neck.Write(neckYaw, -(breathSpine + breathChest) + neckPitch, transform);
        head.Write(headYaw, headPitch, transform);
    }

    Vector3 AimPoint()
    {
        if (playerHead != null)
            return playerHead.position;
        if (player != null)
            return player.position + Vector3.up * 1.55f;
        return transform.position + transform.forward;
    }

    bool WithinRadius(Vector3 aimPoint)
    {
        Vector3 offset = aimPoint - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius && offset.sqrMagnitude > 0.0004f;
    }

    void MeasureLook(Vector3 aimPoint, out float yaw, out float pitch)
    {
        Vector3 to = aimPoint - head.bone.position;
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 flatTo = Vector3.ProjectOnPlane(to, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;
        yaw = flatTo.sqrMagnitude < 0.0001f
            ? 0f
            : Vector3.SignedAngle(flatForward, flatTo, Vector3.up);
        float yawSum = spineYawLimit + neckYawLimit + headYawLimit;
        yaw = Mathf.Clamp(yaw, -yawSum, yawSum);

        Vector3 pitchFrom = flatTo.sqrMagnitude > 0.0001f ? flatTo.normalized : flatForward.normalized;
        pitch = Vector3.SignedAngle(pitchFrom, to, transform.right);
        float pitchSum = spinePitchLimit + neckPitchLimit + headPitchLimit;
        pitch = Mathf.Clamp(pitch, -pitchSum, pitchSum);
    }
}
