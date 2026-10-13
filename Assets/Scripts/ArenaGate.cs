using System;
using UnityEngine;

/// <summary>
/// Swings the entry gate's existing doors around their hinge stiles.
/// They can open for Oraya and close again behind her.
/// </summary>
public class ArenaGate : MonoBehaviour
{
    [SerializeField] Transform leftDoor;
    [SerializeField] Transform rightDoor;
    [SerializeField] Transform leftHinge;
    [SerializeField] Transform rightHinge;
    [SerializeField] float leftOpenAngle = 105f;
    [SerializeField] float rightOpenAngle = -105f;
    [SerializeField, Min(0.1f)] float openDuration = 2.5f;
    [SerializeField] AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public float OpenAmount { get; private set; }
    public bool IsOpen => OpenAmount >= 0.999f;
    public bool IsClosed => OpenAmount <= 0.001f;
    public event Action Opened;
    public event Action Closed;

    Vector3 leftClosedPosition;
    Vector3 rightClosedPosition;
    Quaternion leftClosedRotation;
    Quaternion rightClosedRotation;
    Vector3 leftHingeLocal;
    Vector3 rightHingeLocal;
    bool animating;
    float animFrom;
    float animTo = 1f;
    float animTime;
    bool openedNotified;
    bool closedNotified;

    void Awake()
    {
        if (leftDoor == null)
            leftDoor = transform.Find("Gate_LeftDoor");
        if (rightDoor == null)
            rightDoor = transform.Find("Gate_RightDoor");
        if (leftHinge == null && leftDoor != null)
            leftHinge = leftDoor.Find("HingeStile");
        if (rightHinge == null && rightDoor != null)
            rightHinge = rightDoor.Find("HingeStile");

        if (openCurve == null || openCurve.length == 0)
            openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        CaptureClosedPose();
    }

    public void Open()
    {
        if (animating && animTo >= 0.999f)
            return;
        if (!animating && IsOpen)
            return;

        BeginAnimation(1f);
    }

    public void Close()
    {
        if (animating && animTo <= 0.001f)
            return;
        if (!animating && IsClosed)
            return;

        BeginAnimation(0f);
    }

    public void SetOpenImmediate()
    {
        animating = false;
        Apply(1f);
        NotifyOpened();
    }

    public void SetClosedImmediate()
    {
        animating = false;
        Apply(0f);
        NotifyClosed();
    }

    void BeginAnimation(float target)
    {
        animFrom = OpenAmount;
        animTo = target;
        animTime = 0f;
        animating = true;
    }

    void Update()
    {
        if (!animating)
            return;

        animTime += Time.deltaTime;
        float u = Mathf.Clamp01(animTime / Mathf.Max(0.1f, openDuration));
        float eased = openCurve != null && openCurve.length > 0 ? openCurve.Evaluate(u) : u;
        Apply(Mathf.Lerp(animFrom, animTo, eased));
        if (u < 1f)
            return;

        animating = false;
        Apply(animTo);
        if (animTo >= 0.999f)
            NotifyOpened();
        else
            NotifyClosed();
    }

    void CaptureClosedPose()
    {
        if (leftDoor != null)
        {
            leftClosedPosition = leftDoor.localPosition;
            leftClosedRotation = leftDoor.localRotation;
            leftHingeLocal = leftHinge != null ? leftDoor.InverseTransformPoint(leftHinge.position) : Vector3.zero;
        }

        if (rightDoor != null)
        {
            rightClosedPosition = rightDoor.localPosition;
            rightClosedRotation = rightDoor.localRotation;
            rightHingeLocal = rightHinge != null ? rightDoor.InverseTransformPoint(rightHinge.position) : Vector3.zero;
        }
    }

    void Apply(float amount)
    {
        OpenAmount = Mathf.Clamp01(amount);
        Swing(leftDoor, leftClosedPosition, leftClosedRotation, leftHingeLocal, leftOpenAngle * OpenAmount);
        Swing(rightDoor, rightClosedPosition, rightClosedRotation, rightHingeLocal, rightOpenAngle * OpenAmount);
    }

    static void Swing(Transform door, Vector3 closedPosition, Quaternion closedRotation, Vector3 hingeLocal, float angle)
    {
        if (door == null)
            return;

        door.localPosition = closedPosition;
        door.localRotation = closedRotation;
        if (Mathf.Abs(angle) < 0.01f)
            return;

        Vector3 hingeWorld = door.TransformPoint(hingeLocal);
        door.RotateAround(hingeWorld, Vector3.up, angle);
    }

    void NotifyOpened()
    {
        if (openedNotified)
            return;

        openedNotified = true;
        closedNotified = false;
        Opened?.Invoke();
    }

    void NotifyClosed()
    {
        if (closedNotified)
            return;

        closedNotified = true;
        openedNotified = false;
        Closed?.Invoke();
    }
}
