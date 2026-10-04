using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Locks the cursor while playing so mouse orbit stays inside the Game view.
/// </summary>
public class ThirdPersonCursorLock : MonoBehaviour
{
    void OnEnable()
    {
        SetLocked(true);
    }

    void OnDisable()
    {
        SetLocked(false);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetLocked(false);

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            SetLocked(true);
#endif
    }

    static void SetLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
