using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Temporary HUD control. Deals a fixed amount of damage so the healing potion can be tried.
/// </summary>
public class HealthTestButton : MonoBehaviour
{
    [SerializeField] PlayerHealth health;
    [SerializeField] Button button;
    [SerializeField, Min(1f)] float damage = 30f;

    void Awake()
    {
        if (health == null)
            health = FindAnyObjectByType<PlayerHealth>();
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(Hurt);

        var image = GetComponent<Graphic>();
        if (image != null)
            image.raycastTarget = true;
    }

    void Start()
    {
        var image = GetComponent<Graphic>();
        if (image != null)
            image.raycastTarget = true;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame))
            Hurt();
#endif
    }

    public void Hurt()
    {
        if (health != null)
            health.TakeDamage(damage);
    }
}
