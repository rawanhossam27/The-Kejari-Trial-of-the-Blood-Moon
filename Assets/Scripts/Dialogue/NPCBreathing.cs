using UnityEngine;

/// <summary>
/// Subtle breathing on the upper body. Every pose is rebuilt from the bind rotation,
/// so the motion cannot drift. Neck pitch cancels the spine and chest nod so the head stays level.
/// </summary>
[DefaultExecutionOrder(-20)]
public class NPCBreathing : MonoBehaviour
{
    [SerializeField] string spineBoneName = "Spine";
    [SerializeField] string chestBoneName = "Chest";
    [SerializeField] string neckBoneName = "Neck";
    [SerializeField] string leftShoulderBoneName = "Left shoulder";
    [SerializeField] string rightShoulderBoneName = "Right shoulder";
    [SerializeField, Min(0f)] float chestDegrees = 1.6f;
    [SerializeField, Min(0f)] float spineDegrees = 0.6f;
    [SerializeField, Range(0f, 1f)] float shoulderDegrees = 0.55f;
    [SerializeField, Min(0.05f)] float cycleSeconds = 3.4f;
    [SerializeField, Min(0f)] float capeRootDegrees = 1.1f;
    [SerializeField, Min(0f)] float capeMidDegrees = 1.7f;
    [SerializeField, Min(0f)] float capeTipDegrees = 2.3f;
    [SerializeField, Min(0f)] float capeMidDelay = 0.18f;
    [SerializeField, Min(0f)] float capeTipDelay = 0.36f;
    [SerializeField, Range(0.05f, 0.45f)] float armOut = 0.16f;
    [SerializeField, Range(-0.05f, 0.3f)] float armForward = 0.05f;
    [SerializeField, Range(0f, 30f)] float elbowBend = 8f;
    [SerializeField, Range(0f, 25f)] float wristFlex = 4f;
    [SerializeField, Range(0f, 3f)] float armSwayDegrees = 1.1f;

    PoseBone spine;
    PoseBone chest;
    PoseBone neck;
    PoseBone leftShoulder;
    PoseBone rightShoulder;
    PoseBone capeLeft1;
    PoseBone capeLeft2;
    PoseBone capeLeft3;
    PoseBone capeRight1;
    PoseBone capeRight2;
    PoseBone capeRight3;
    ArmRest leftArm;
    ArmRest rightArm;

    /// <summary>Spine nod applied this frame, in degrees. Head tracking adds this back when it rewrites the spine.</summary>
    public float SpinePitch { get; private set; }

    /// <summary>Chest nod applied this frame, in degrees.</summary>
    public float ChestPitch { get; private set; }

    void Awake()
    {
        spine = PoseBone.Capture(transform, spineBoneName);
        chest = PoseBone.Capture(transform, chestBoneName);
        neck = PoseBone.Capture(transform, neckBoneName);
        leftShoulder = PoseBone.Capture(transform, leftShoulderBoneName);
        rightShoulder = PoseBone.Capture(transform, rightShoulderBoneName);
        capeLeft1 = PoseBone.Capture(transform, "Cape_left_1");
        capeLeft2 = PoseBone.Capture(transform, "Cape_left_2");
        capeLeft3 = PoseBone.Capture(transform, "Cape_left_3");
        capeRight1 = PoseBone.Capture(transform, "Cape_right_1");
        capeRight2 = PoseBone.Capture(transform, "Cape_right_2");
        capeRight3 = PoseBone.Capture(transform, "Cape_right_3");
        leftArm = ArmRest.Capture(transform, "Left arm", "Left elbow", "Left wrist", "Arm_Twist_Offset_l", -1f);
        rightArm = ArmRest.Capture(transform, "Right arm", "Right elbow", "Right wrist", "Arm_Twist_Offset_r", 1f);
    }

    void LateUpdate()
    {
        float wave = Mathf.Sin(Time.time / cycleSeconds * Mathf.PI * 2f);
        SpinePitch = wave * spineDegrees;
        ChestPitch = wave * chestDegrees;
        float shoulder = wave * Mathf.Min(shoulderDegrees, 1f);

        spine.Write(0f, SpinePitch, transform);
        chest.Write(0f, ChestPitch, transform);
        neck.Write(0f, -(SpinePitch + ChestPitch), transform);
        leftShoulder.Write(0f, shoulder, transform);
        rightShoulder.Write(0f, shoulder, transform);

        WriteCape(capeLeft1, Wave(0f) * capeRootDegrees);
        WriteCape(capeLeft2, Wave(capeMidDelay) * capeMidDegrees);
        WriteCape(capeLeft3, Wave(capeTipDelay) * capeTipDegrees);
        WriteCape(capeRight1, Wave(0f) * capeRootDegrees);
        WriteCape(capeRight2, Wave(capeMidDelay) * capeMidDegrees);
        WriteCape(capeRight3, Wave(capeTipDelay) * capeTipDegrees);

        float sway = wave * armSwayDegrees;
        leftArm.Write(transform, armOut, armForward, elbowBend, wristFlex, sway);
        rightArm.Write(transform, armOut, armForward, elbowBend, wristFlex, sway);
    }

    float Wave(float delay)
    {
        return Mathf.Sin((Time.time - delay) / cycleSeconds * Mathf.PI * 2f);
    }

    void WriteCape(PoseBone bone, float pitch)
    {
        bone.Write(0f, pitch, transform);
    }
}

/// <summary>Bind pose for one bone. Offsets are applied from this pose every frame.</summary>
struct PoseBone
{
    public Transform bone;
    public Quaternion baseLocal;
    public Quaternion bindWorld;
    public bool found;

    public static PoseBone Capture(Transform root, string boneName)
    {
        PoseBone pose = new PoseBone();
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name != boneName)
                continue;
            pose.bone = candidate;
            pose.baseLocal = candidate.localRotation;
            pose.bindWorld = candidate.rotation;
            pose.found = true;
            break;
        }
        return pose;
    }

    public void Write(float yawDegrees, float pitchDegrees, Transform character)
    {
        if (!found)
            return;
        bone.localRotation = Posed(yawDegrees, pitchDegrees, character);
    }

    public Quaternion Posed(float yawDegrees, float pitchDegrees, Transform character)
    {
        Vector3 yawAxis = Quaternion.Inverse(bindWorld) * Vector3.up;
        Vector3 pitchAxis = Quaternion.Inverse(bindWorld) * character.right;
        Quaternion offset = Quaternion.AngleAxis(yawDegrees, yawAxis) * Quaternion.AngleAxis(pitchDegrees, pitchAxis);
        return baseLocal * offset;
    }

    public void WriteWorld(Quaternion worldRotation)
    {
        if (!found || bone.parent == null)
            return;
        bone.localRotation = Quaternion.Inverse(bone.parent.rotation) * worldRotation;
    }
}

/// <summary>
/// Upper arm, elbow, and wrist posed from their bind directions every frame.
/// </summary>
struct ArmRest
{
    public PoseBone upper;
    public PoseBone elbow;
    public PoseBone wrist;
    public PoseBone twist;
    public Vector3 bindArmDir;
    public Vector3 bindForeDir;
    public float side;

    public static ArmRest Capture(Transform root, string arm, string elbow, string wrist, string twist, float side)
    {
        ArmRest rest = new ArmRest();
        rest.upper = PoseBone.Capture(root, arm);
        rest.elbow = PoseBone.Capture(root, elbow);
        rest.wrist = PoseBone.Capture(root, wrist);
        rest.twist = PoseBone.Capture(root, twist);
        rest.side = side;
        if (rest.upper.found && rest.elbow.found)
            rest.bindArmDir = (rest.elbow.bone.position - rest.upper.bone.position).normalized;
        if (rest.elbow.found && rest.wrist.found)
            rest.bindForeDir = (rest.wrist.bone.position - rest.elbow.bone.position).normalized;
        return rest;
    }

    public void Write(Transform character, float armOut, float armForward, float elbowBend, float wristFlex, float swayDegrees)
    {
        if (!upper.found || bindArmDir.sqrMagnitude < 0.5f)
            return;

        Vector3 desired = (Vector3.down + character.right * side * armOut + character.forward * armForward).normalized;
        Quaternion drop = Quaternion.FromToRotation(bindArmDir, desired);
        Quaternion sway = Quaternion.AngleAxis(swayDegrees, character.right);
        Quaternion armSwing = sway * drop;
        upper.WriteWorld(armSwing * upper.bindWorld);
        if (twist.found)
            twist.WriteWorld(armSwing * twist.bindWorld);

        if (!elbow.found || bindForeDir.sqrMagnitude < 0.5f)
            return;

        Vector3 posedFore = armSwing * bindForeDir;
        Vector3 bendAxis = BendAxis(posedFore, character);
        elbow.WriteWorld(Quaternion.AngleAxis(elbowBend, bendAxis) * armSwing * elbow.bindWorld);
        if (wrist.found)
            wrist.WriteWorld(Quaternion.AngleAxis(elbowBend + wristFlex, bendAxis) * armSwing * wrist.bindWorld);
    }

    static Vector3 BendAxis(Vector3 forearm, Transform character)
    {
        Vector3 axis = Vector3.Cross(forearm, character.forward);
        if (axis.sqrMagnitude < 0.0001f)
            axis = character.right;
        axis.Normalize();
        if (Vector3.Dot(axis, character.right) < 0f)
            axis = -axis;
        Vector3 bent = Quaternion.AngleAxis(10f, axis) * forearm;
        if (Vector3.Dot(bent, character.forward) < Vector3.Dot(forearm, character.forward))
            axis = -axis;
        return axis;
    }
}
