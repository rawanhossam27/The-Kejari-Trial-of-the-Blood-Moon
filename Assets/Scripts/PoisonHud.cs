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
    int shownHealing;
    string acquiredText = "Poison Acquired";
    PoisonVialPickup[] vials;
    HealingPotion healing;

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
        if (healing == null)
            healing = FindAnyObjectByType<HealingPotion>();
        if (healing != null)
            healing.ChargesChanged += HandleHealingChanged;
        vials = FindObjectsByType<PoisonVialPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    void OnDisable()
    {
        if (poison != null)
            poison.ChargesChanged -= HandleChargesChanged;
        if (healing != null)
            healing.ChargesChanged -= HandleHealingChanged;
    }

    void HandleChargesChanged(int charges)
    {
        if (charges > shownCharges && shownCharges >= 0)
        {
            acquiredText = "Poison Acquired";
            acquiredUntil = Time.time + acquiredDuration;
        }
        shownCharges = charges;
    }

    void HandleHealingChanged(int charges)
    {
        if (charges > shownHealing && shownHealing >= 0)
        {
            acquiredText = "Healing Potion Acquired";
            acquiredUntil = Time.time + acquiredDuration;
        }
        shownHealing = charges;
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
            SetPrompt(acquiredText);
            Show(statusGroup, false);
            return;
        }

        var closest = ArenaPickups.ClosestInRange();
        if (closest != null)
        {
            Show(statusGroup, false);
            SetPrompt(closest.Prompt);
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
        bool healingReady = healing != null && healing.Charges > 0;
        bool poisonReady = poison != null && poison.Charges > 0;
        if (healingReady || poisonReady)
        {
            string text = healingReady
                ? (healing.Charges == 1 ? "Press H to heal" : $"Press H to heal  x{healing.Charges}")
                : "";
            if (poisonReady)
                text = text.Length == 0 ? "[Q]  Poison Moon Sword" : text + "\n[Q]  Poison Moon Sword";
            SetPrompt(text);
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
