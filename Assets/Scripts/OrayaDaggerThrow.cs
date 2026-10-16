using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// R plays a procedural left-arm throw, then releases the dagger toward the mouse aim point.
/// While the cursor is locked, the mouse orbits the camera and the dagger flies at the center of the view.
/// Offsets are applied in LateUpdate on top of the Animator's pose and are not stored on the bones.
/// </summary>
[DefaultExecutionOrder(200)]
public class OrayaDaggerThrow : MonoBehaviour
{
    enum Phase
    {
        Idle,
        Windup,
        Swing,
        Recover,
        Reequip
    }

    [SerializeField] Transform handDagger;
    [SerializeField] DaggerProjectile projectilePrefab;
    [SerializeField] OrayaMovement movement;
    [SerializeField] OrayaAttack attack;
    [SerializeField] PlayerHealth health;
    [SerializeField, Min(1f)] float speed = 22f;
    [SerializeField, Min(0f)] float damage = 1f;
    [SerializeField, Min(0.2f)] float lifetime = 2.5f;
    [SerializeField, Min(1f)] float aimRange = 24f;
    [SerializeField, Min(0.1f)] float cooldown = 1.5f;
    [SerializeField, Min(0f)] float releaseClearance = 0.12f;

    [Header("Throw timing")]
    [SerializeField, Min(0.05f)] float windupDuration = 0.35f;
    [SerializeField, Min(0.05f)] float swingDuration = 0.2f;
    [SerializeField, Min(0.05f)] float recoveryDuration = 0.4f;
    [SerializeField, Min(0.05f)] float reequipDuration = 0.25f;

    [Header("Left arm offsets (degrees)")]
    [SerializeField] float upperWindupZ = -58f;
    [SerializeField] float upperReleaseZ = 64f;
    [SerializeField] float upperWindupY = 12f;
    [SerializeField] float upperReleaseY = 26f;
    [SerializeField] float elbowWindupZ = -52f;
    [SerializeField] float elbowReleaseZ = 34f;
    [SerializeField] float clavicleWindupZ = -18f;
    [SerializeField] float clavicleReleaseZ = 12f;
    [SerializeField, Range(0f, 1.2f)] float aimSteer = 0.55f;
    [SerializeField, Range(0f, 1f)] float handWindupWeight = 0.4f;
    [SerializeField, Range(0f, 1f)] float handReleaseWeight = 0.85f;

    const string UpperPath = "root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_l/upperarm_l";
    const float TipLocalDistance = 0.93f;

    Transform clavicle;
    Transform upperArm;
    Transform lowerArm;
    Transform hand;
    Phase phase = Phase.Idle;
    float motionTime;
    float nextThrowTime;
    bool released;
    bool handEquipped = true;
    Vector3 aimPoint;
    Vector3 restLocalPosition;
    Vector3 restLocalScale;
    Quaternion restLocalRotation;
    Renderer[] handRenderers;

    void Awake()
    {
        if (movement == null)
            movement = GetComponent<OrayaMovement>();
        if (attack == null)
            attack = GetComponent<OrayaAttack>();
        if (health == null)
            health = GetComponent<PlayerHealth>();

        upperArm = transform.Find(UpperPath);
        clavicle = upperArm != null ? upperArm.parent : null;
        lowerArm = upperArm != null ? upperArm.Find("lowerarm_l") : null;
        hand = lowerArm != null ? lowerArm.Find("hand_l") : null;
        if (handDagger == null && lowerArm != null)
            handDagger = lowerArm.Find("hand_l/weapon_l/RubyDagger");
        if (handDagger != null)
        {
            handRenderers = handDagger.GetComponentsInChildren<Renderer>(true);
            restLocalPosition = handDagger.localPosition;
            restLocalScale = handDagger.localScale;
            restLocalRotation = handDagger.localRotation;
        }
    }

    void Update()
    {
        if (phase == Phase.Idle)
        {
            if (ThrowPressed() && Time.time >= nextThrowTime && CanThrow())
                Begin();
            else if (!handEquipped && Time.time >= nextThrowTime)
                BeginReequip();
            return;
        }

        if (phase == Phase.Reequip)
        {
            if (ThrowPressed() && Time.time >= nextThrowTime && CanThrow())
            {
                Begin();
                return;
            }

            motionTime += Time.deltaTime;
            if (motionTime >= reequipDuration)
                FinishReequip();
            return;
        }

        motionTime += Time.deltaTime;
        if (phase == Phase.Windup && motionTime >= windupDuration)
        {
            phase = Phase.Swing;
            motionTime = 0f;
        }
        else if (phase == Phase.Recover && motionTime >= recoveryDuration)
        {
            phase = Phase.Idle;
            motionTime = 0f;
        }
    }

    void LateUpdate()
    {
        if (phase == Phase.Reequip)
        {
            ApplyReequip();
            return;
        }

        if (phase == Phase.Idle || upperArm == null || lowerArm == null)
            return;

        Camera camera = Camera.main;
        if (camera != null && handDagger != null)
            aimPoint = TargetPoint(camera, out _);

        float steer = AimSteer();
        Pose(steer, out float upperZ, out float upperY, out float elbowZ, out float clavZ, out float handWeight, out Vector3 desiredTip);
        if (clavicle != null)
            clavicle.localRotation *= Quaternion.Euler(0f, 0f, clavZ);
        upperArm.localRotation *= Quaternion.Euler(0f, upperY, upperZ);
        lowerArm.localRotation *= Quaternion.Euler(0f, 0f, elbowZ);
        PointDagger(desiredTip, handWeight);

        if (phase == Phase.Swing && !released && motionTime >= swingDuration)
            Release();
    }

    public bool CanThrow()
    {
        if (phase != Phase.Idle && phase != Phase.Reequip)
            return false;
        if (handDagger == null || upperArm == null || lowerArm == null || projectilePrefab == null || Camera.main == null)
            return false;
        if (health != null && health.IsDead)
            return false;
        if (movement != null && (!movement.InputEnabled || movement.CinematicMoveActive))
            return false;
        if (attack != null && attack.IsAttacking)
            return false;
        return true;
    }

    void Begin()
    {
        SnapDaggerToRest();
        SetHandVisible(true);
        handEquipped = true;
        Camera camera = Camera.main;
        aimPoint = camera != null ? TargetPoint(camera, out _) : transform.position + transform.forward * aimRange;
        phase = Phase.Windup;
        motionTime = 0f;
        released = false;
        nextThrowTime = Time.time + cooldown;
    }

    void BeginReequip()
    {
        phase = Phase.Reequip;
        motionTime = 0f;
        SetHandVisible(true);
    }

    void ApplyReequip()
    {
        if (handDagger == null)
            return;

        float t = Smooth(motionTime / reequipDuration);
        Vector3 tucked = restLocalPosition + new Vector3(0f, -0.05f, 0.03f);
        handDagger.localPosition = Vector3.Lerp(tucked, restLocalPosition, t);
        handDagger.localScale = Vector3.Lerp(restLocalScale * 0.6f, restLocalScale, t);
        handDagger.localRotation = restLocalRotation;
    }

    void FinishReequip()
    {
        SnapDaggerToRest();
        SetHandVisible(true);
        handEquipped = true;
        phase = Phase.Idle;
        motionTime = 0f;
    }

    void SnapDaggerToRest()
    {
        if (handDagger == null)
            return;

        handDagger.localPosition = restLocalPosition;
        handDagger.localScale = restLocalScale;
        handDagger.localRotation = restLocalRotation;
    }

    float AimSteer()
    {
        Vector3 toAim = aimPoint - transform.position;
        toAim.y = 0f;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (toAim.sqrMagnitude < 0.04f || forward.sqrMagnitude < 0.04f)
            return 0f;

        float yaw = Mathf.Clamp(Vector3.SignedAngle(forward, toAim, Vector3.up), -80f, 80f);
        return Mathf.Clamp(-yaw * aimSteer, -40f, 45f);
    }

    void Pose(float steer, out float upperZ, out float upperY, out float elbowZ, out float clavZ, out float handWeight, out Vector3 desiredTip)
    {
        float windupY = upperWindupY + steer * 0.4f;
        float releaseY = upperReleaseY + steer;
        Vector3 aimDir = handDagger != null ? aimPoint - handDagger.position : transform.forward;
        if (aimDir.sqrMagnitude < 0.0001f)
            aimDir = transform.forward;
        aimDir.Normalize();
        Vector3 flat = Vector3.ProjectOnPlane(aimDir, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
            flat = transform.forward;
        Vector3 cock = (-flat.normalized + Vector3.up * 1.15f).normalized;

        if (phase == Phase.Windup)
        {
            float t = Prepare(motionTime / windupDuration);
            upperZ = Mathf.Lerp(0f, upperWindupZ, t);
            upperY = Mathf.Lerp(0f, windupY, t);
            elbowZ = Mathf.Lerp(0f, elbowWindupZ, t);
            clavZ = Mathf.Lerp(0f, clavicleWindupZ, t);
            handWeight = Mathf.Lerp(0f, handWindupWeight, t);
            desiredTip = cock;
            return;
        }

        if (phase == Phase.Swing)
        {
            float t = Accelerate(motionTime / swingDuration);
            upperZ = Mathf.Lerp(upperWindupZ, upperReleaseZ, t);
            upperY = Mathf.Lerp(windupY, releaseY, t);
            elbowZ = Mathf.Lerp(elbowWindupZ, elbowReleaseZ, t);
            clavZ = Mathf.Lerp(clavicleWindupZ, clavicleReleaseZ, t);
            handWeight = Mathf.Lerp(handWindupWeight, handReleaseWeight, t);
            desiredTip = Vector3.Slerp(cock, aimDir, t);
            return;
        }

        float recover = Smooth(motionTime / recoveryDuration);
        upperZ = Mathf.Lerp(upperReleaseZ, 0f, recover);
        upperY = Mathf.Lerp(releaseY, 0f, recover);
        elbowZ = Mathf.Lerp(elbowReleaseZ, 0f, recover);
        clavZ = Mathf.Lerp(clavicleReleaseZ, 0f, recover);
        handWeight = Mathf.Lerp(handReleaseWeight, 0f, recover);
        desiredTip = aimDir;
    }

    void PointDagger(Vector3 desiredTip, float weight)
    {
        if (hand == null || handDagger == null || weight <= 0.001f || desiredTip.sqrMagnitude < 0.0001f)
            return;

        Quaternion align = Quaternion.FromToRotation(handDagger.up, desiredTip.normalized);
        hand.rotation = Quaternion.Slerp(hand.rotation, align * hand.rotation, weight);
    }

    void Release()
    {
        released = true;
        phase = Phase.Recover;
        motionTime = 0f;

        Camera camera = Camera.main;
        if (camera == null || handDagger == null || projectilePrefab == null)
            return;
        if (health != null && health.IsDead)
            return;
        if (movement != null && movement.CinematicMoveActive)
            return;

        Vector3 tip = TipWorld();
        Vector3 target = TargetPoint(camera, out Ray aimRay);
        Vector3 origin = ReleasePoint(aimRay, tip, target);
        Vector3 direction = target - origin;
        if (direction.sqrMagnitude < 0.0001f)
            direction = aimRay.GetPoint(aimRange) - origin;
        direction.Normalize();
        aimPoint = target;

        DaggerProjectile dagger = Instantiate(projectilePrefab, origin, Quaternion.identity);
        dagger.transform.SetParent(null, true);
        dagger.transform.localScale = handDagger.lossyScale;
        var renderer = dagger.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = true;
        dagger.Launch(direction, speed, damage, lifetime, transform);
        SetHandVisible(false);
        handEquipped = false;
    }

    Vector3 ReleasePoint(Ray aimRay, Vector3 tip, Vector3 target)
    {
        float tipAlong = Vector3.Dot(tip - aimRay.origin, aimRay.direction);
        float targetAlong = Vector3.Dot(target - aimRay.origin, aimRay.direction);
        Vector3 chest = transform.position + Vector3.up * 1.15f;
        float chestAlong = Vector3.Dot(chest - aimRay.origin, aimRay.direction) + 0.3f;
        float along = Mathf.Clamp(tipAlong, chestAlong, Mathf.Max(chestAlong, targetAlong - 0.45f));
        Vector3 onRay = aimRay.origin + aimRay.direction * along;
        return Vector3.Distance(tip, onRay) < 0.2f ? tip + aimRay.direction * releaseClearance : onRay;
    }

    Vector3 TipWorld()
    {
        float reach = TipLocalDistance * Mathf.Abs(handDagger.lossyScale.y);
        return handDagger.position + handDagger.up * reach;
    }

    Vector3 TargetPoint(Camera camera, out Ray ray)
    {
        ray = AimRay(camera);
        float traveled = 0f;
        while (traveled < aimRange)
        {
            float distance = aimRange - traveled;
            if (!Physics.Raycast(ray.origin + ray.direction * traveled, ray.direction, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                break;

            bool self = hit.transform == transform || hit.transform.IsChildOf(transform);
            bool lens = traveled + hit.distance < 0.45f;
            if (!self && !lens)
                return hit.point;

            traveled += hit.distance + 0.05f;
        }

        return ray.GetPoint(aimRange);
    }

    Ray AimRay(Camera camera)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.None)
        {
            Vector2 screen = mouse.position.ReadValue();
            if (screen.x >= 0f && screen.y >= 0f && screen.x <= camera.pixelWidth && screen.y <= camera.pixelHeight)
                return camera.ScreenPointToRay(screen);
        }
#endif
        return camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    void SetHandVisible(bool visible)
    {
        if (handRenderers == null)
            return;

        for (int i = 0; i < handRenderers.Length; i++)
        {
            if (handRenderers[i] != null)
                handRenderers[i].enabled = visible;
        }
    }

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    static float Prepare(float t)
    {
        t = Mathf.Clamp01(t);
        float settle = Mathf.Clamp01(t / 0.72f);
        return 1f - (1f - settle) * (1f - settle);
    }

    static float Accelerate(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    static bool ThrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
