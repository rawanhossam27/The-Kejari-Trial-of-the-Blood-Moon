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
        if (Application.isPlaying)
            SetLocked(true);
    }

    void OnDisable()
    {
        SetLocked(false);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (OrayaMovement.GameplaySuspended)
            return;

        Keyboard keyboard = Keyboard.current;
        // Level 2 pause owns Escape. Other scenes still use it only to release the cursor.
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !LevelPauseMenu.OwnsEscape)
            SetLocked(false);

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Cursor.lockState == CursorLockMode.Locked)
            return;
        if (PointerOverUi())
            return;

        SetLocked(true);
#endif
    }

    static bool PointerOverUi()
    {
        var system = UnityEngine.EventSystems.EventSystem.current;
        return system != null && system.IsPointerOverGameObject();
    }

    static void SetLocked(bool locked)
    {
        // Confined keeps the cursor inside the game view for the orbit, and still lets it reach HUD buttons.
        Cursor.lockState = locked ? CursorLockMode.Confined : CursorLockMode.None;
        Cursor.visible = true;
    }
}
