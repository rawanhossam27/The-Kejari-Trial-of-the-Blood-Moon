using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mouse look for the lying-down camera. The body never moves.
/// The cursor stays confined, the same way the rest of the game keeps it in the Game view.
/// </summary>
[DisallowMultipleComponent]
public class BedRestLook : MonoBehaviour
{
    [SerializeField] Camera view;
    [SerializeField] float sensitivity = 0.08f;
    [SerializeField] float minPitch = -28f;
    [SerializeField] float maxPitch = 42f;
    [SerializeField] float minYaw = -85f;
    [SerializeField] float maxYaw = 85f;

    Vector3 anchor;
    Quaternion baseRotation;
    float yaw;
    float pitch;
    bool active;
    bool suspended;
    bool focusing;
    float focusFromYaw;
    float focusFromPitch;
    float focusYaw;
    float focusPitch;
    float focusDuration = 0.6f;
    float focusElapsed;

    public bool LookEnabled => active && !suspended;
    public bool IsFocusing => focusing;
    public Camera View => view;

    void Awake()
    {
        if (view == null)
            view = GetComponent<Camera>();
    }

    /// <summary>Call once the blink has finished. The current camera pose becomes the center of the look.</summary>
    public void Begin()
    {
        if (view == null)
            return;

        anchor = view.transform.position;
        baseRotation = view.transform.rotation;
        yaw = 0f;
        pitch = 0f;
        active = true;
        suspended = false;
        ConfineCursor(true);
        Apply();
    }

    /// <summary>Dialogue needs the cursor. The view stays where she was looking.</summary>
    public void SetSuspended(bool value)
    {
        suspended = value;
        if (!active)
            return;

        ConfineCursor(!value);
    }

    /// <summary>
    /// Turns the resting view onto a point, then leaves it there.
    /// Mouse look waits until the turn finishes.
    /// </summary>
    public void FocusOn(Vector3 worldPoint, float seconds)
    {
        if (view == null || !active)
            return;

        Vector3 to = worldPoint - anchor;
        if (to.sqrMagnitude < 0.0001f)
            return;

        Vector3 local = Quaternion.Inverse(baseRotation) * to.normalized;
        float flat = new Vector2(local.x, local.z).magnitude;
        focusFromYaw = yaw;
        focusFromPitch = pitch;
        focusYaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, minYaw, maxYaw);
        focusPitch = Mathf.Clamp(-Mathf.Atan2(local.y, flat) * Mathf.Rad2Deg, minPitch, maxPitch);
        focusDuration = Mathf.Max(0.05f, seconds);
        focusElapsed = 0f;
        focusing = true;
        Apply();
    }

    /// <summary>Same path as the mouse. Used so the look can be checked without a physical mouse.</summary>
    public void AddLook(float yawDelta, float pitchDelta)
    {
        if (!active || suspended)
            return;

        yaw = Mathf.Clamp(yaw + yawDelta, minYaw, maxYaw);
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
        Apply();
    }

    void LateUpdate()
    {
        if (!active || view == null)
            return;

        if (focusing)
        {
            focusElapsed += Time.deltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(focusElapsed / focusDuration));
            yaw = Mathf.Lerp(focusFromYaw, focusYaw, blend);
            pitch = Mathf.Lerp(focusFromPitch, focusPitch, blend);
            Apply();
            if (blend >= 1f)
                focusing = false;
            return;
        }

        bool talking = DialogueManager.Instance != null && DialogueManager.Instance.IsRunning;
        if (talking != suspended)
            SetSuspended(talking);

        if (suspended)
            return;

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            AddLook(delta.x * sensitivity, -delta.y * sensitivity);
            return;
        }
#endif
        Apply();
    }

    void Apply()
    {
        view.transform.position = anchor;
        view.transform.rotation = baseRotation * Quaternion.Euler(pitch, yaw, 0f);
    }

    static void ConfineCursor(bool confined)
    {
        Cursor.lockState = confined ? CursorLockMode.Confined : CursorLockMode.None;
        Cursor.visible = true;
    }
}
