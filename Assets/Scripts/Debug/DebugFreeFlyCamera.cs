using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Temporary Play Mode free-fly camera for arena inspection.
/// </summary>
public class DebugFreeFlyCamera : MonoBehaviour
{
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float fastMoveMultiplier = 3f;
    [SerializeField] float mouseSensitivity = 2f;

    float yaw;
    float pitch;
    bool lookEnabled;

    void OnEnable()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f)
            pitch -= 360f;
        SetLookEnabled(true);
    }

    void OnDisable()
    {
        SetLookEnabled(false);
    }

    void Update()
    {
        if (WasEscapePressed())
            SetLookEnabled(false);
        else if (!lookEnabled && WasPrimaryClickPressed())
            SetLookEnabled(true);

        if (lookEnabled)
            ApplyMouseLook();

        ApplyMove();
    }

    void ApplyMouseLook()
    {
        Vector2 delta = ReadMouseDelta();
        yaw += delta.x * mouseSensitivity * 0.08f;
        pitch -= delta.y * mouseSensitivity * 0.08f;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void ApplyMove()
    {
        Vector3 local = ReadMoveInput();
        if (local.sqrMagnitude > 1f)
            local.Normalize();

        float speed = moveSpeed;
        if (IsFastPressed())
            speed *= fastMoveMultiplier;

        transform.position += transform.TransformDirection(local) * (speed * Time.deltaTime);
    }

    void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

    static Vector3 ReadMoveInput()
    {
        float x = 0f;
        float y = 0f;
        float z = 0f;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            if (kb.qKey.isPressed) y -= 1f;
            if (kb.eKey.isPressed) y += 1f;
            if (kb.sKey.isPressed) z -= 1f;
            if (kb.wKey.isPressed) z += 1f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (x == 0f && y == 0f && z == 0f)
        {
            x = Input.GetAxisRaw("Horizontal");
            z = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.Q)) y -= 1f;
            if (Input.GetKey(KeyCode.E)) y += 1f;
            if (Input.GetKey(KeyCode.A)) x = -1f;
            if (Input.GetKey(KeyCode.D)) x = 1f;
            if (Input.GetKey(KeyCode.S)) z = -1f;
            if (Input.GetKey(KeyCode.W)) z = 1f;
        }
#endif
        return new Vector3(x, y, z);
    }

    static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
            return mouse.delta.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 20f;
#endif
#pragma warning disable CS0162
        return Vector2.zero;
#pragma warning restore CS0162
    }

    static bool IsFastPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
#pragma warning disable CS0162
        return false;
#pragma warning restore CS0162
    }

    static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#endif
#pragma warning disable CS0162
        return false;
#pragma warning restore CS0162
    }

    static bool WasPrimaryClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#endif
#pragma warning disable CS0162
        return false;
#pragma warning restore CS0162
    }
}
