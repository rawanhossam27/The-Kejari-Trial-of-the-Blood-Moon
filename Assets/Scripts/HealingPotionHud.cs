using TMPro;
using UnityEngine;

/// <summary>
/// Shows a carried healing potion under the existing health plate.
/// </summary>
public class HealingPotionHud : MonoBehaviour
{
    [SerializeField] HealingPotion potion;
    [SerializeField] OrayaMovement movement;
    [SerializeField] TMP_Text label;

    void Awake()
    {
        if (potion == null)
            potion = FindAnyObjectByType<HealingPotion>();
        if (movement == null && potion != null)
            movement = potion.GetComponent<OrayaMovement>();
        if (label != null)
            label.raycastTarget = false;
    }

    void Update()
    {
        if (label == null)
            return;

        bool gameplay = movement == null || (movement.InputEnabled && !movement.CinematicMoveActive);
        bool available = gameplay && potion != null && potion.Charges > 0;
        label.gameObject.SetActive(available);
        if (!available)
            return;

        label.text = potion.Charges == 1
            ? "Press H to heal"
            : $"Press H to heal  x{potion.Charges}";
    }
}
