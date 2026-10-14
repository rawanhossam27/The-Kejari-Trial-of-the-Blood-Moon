using TMPro;
using UnityEngine;

/// <summary>
/// Bottom-center poison prompts on the existing gameplay canvas.
/// </summary>
public class PoisonHud : MonoBehaviour
{
    [SerializeField] PoisonVialPickup vial;
    [SerializeField] MoonSwordPoison poison;
    [SerializeField] OrayaMovement movement;
    [SerializeField] CanvasGroup promptGroup;
    [SerializeField] TMP_Text promptLabel;
    [SerializeField] CanvasGroup statusGroup;
    [SerializeField] TMP_Text statusLabel;
    [SerializeField] UnityEngine.UI.Image statusFill;
    [SerializeField, Min(0.2f)] float acquiredDuration = 2.4f;

    float acquiredUntil;
    int shownCharges;
    PoisonVialPickup[] vials;

    void Awake()
    {
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            graphic.raycastTarget = false;
        if (promptGroup != null)
        {
            promptGroup.alpha = 0f;
            promptGroup.blocksRaycasts = false;
            promptGroup.interactable = false;
        }
        if (statusGroup != null)
        {
            statusGroup.alpha = 0f;
            statusGroup.blocksRaycasts = false;
            statusGroup.interactable = false;
        }
    }

    void OnEnable()
    {
        if (poison != null)
            poison.ChargesChanged += HandleChargesChanged;
        vials = FindObjectsByType<PoisonVialPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    void OnDisable()
    {
        if (poison != null)
            poison.ChargesChanged -= HandleChargesChanged;
    }

    void HandleChargesChanged(int charges)
    {
        if (charges > shownCharges && shownCharges >= 0)
            acquiredUntil = Time.time + acquiredDuration;
        shownCharges = charges;
    }

    void Update()
    {
        bool gameplay = movement == null || (movement.InputEnabled && !movement.CinematicMoveActive);
        if (!gameplay)
        {
            Show(promptGroup, false);
            Show(statusGroup, false);
            return;
        }

        if (Time.time < acquiredUntil)
        {
            SetPrompt("Poison Acquired");
            Show(statusGroup, false);
            return;
        }

        if (poison != null && poison.IsActive)
        {
            Show(promptGroup, false);
            Show(statusGroup, true);
            if (statusLabel != null)
                statusLabel.text = $"POISONED  —  {Mathf.CeilToInt(poison.Remaining)}s";
            if (statusFill != null)
                statusFill.fillAmount = poison.Duration <= 0f ? 0f : poison.Remaining / poison.Duration;
            return;
        }

        Show(statusGroup, false);
        if (poison != null && poison.Charges > 0)
        {
            SetPrompt("[Q]  Poison Moon Sword");
            return;
        }

        if (AnyVialInRange())
        {
            SetPrompt("[E]  Collect Poison");
            return;
        }

        Show(promptGroup, false);
    }

    bool AnyVialInRange()
    {
        if (vials == null)
            return vial != null && vial.InRange;

        for (int i = 0; i < vials.Length; i++)
        {
            if (vials[i] != null && vials[i].InRange)
                return true;
        }

        return false;
    }

    void SetPrompt(string text)
    {
        if (promptLabel != null)
            promptLabel.text = text;
        Show(promptGroup, true);
    }

    static void Show(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;
        group.alpha = visible ? 1f : 0f;
    }
}
