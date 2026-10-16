using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One world healing potion. E collects it once, and only if it is the closest pickup.
/// </summary>
public class HealingPotionPickup : MonoBehaviour, IArenaPickup
{
    [SerializeField] Transform player;
    [SerializeField] OrayaMovement movement;
    [SerializeField] HealingPotion potion;
    [SerializeField, Min(0.2f)] float range = 2f;
    [SerializeField, Min(0f)] float bobHeight = 0.12f;
    [SerializeField, Min(0f)] float bobSpeed = 1.7f;
    [SerializeField] float spinSpeed = 22f;

    Vector3 restPosition;
    float phase;
    bool restCaptured;
    bool collected;

    public bool IsCollected => collected;
    public string Prompt => "Press E to collect Healing Potion";

    public bool InRange
    {
        get
        {
            if (collected || player == null || !GameplayAllowed())
                return false;
            return (player.position - transform.position).sqrMagnitude <= range * range;
        }
    }

    public float DistanceSqr
    {
        get
        {
            if (player == null)
                return float.MaxValue;
            return (player.position - transform.position).sqrMagnitude;
        }
    }

    void Awake()
    {
        if (player == null)
        {
            var oraya = GameObject.Find("Oraya");
            if (oraya != null)
                player = oraya.transform;
        }

        if (movement == null && player != null)
            movement = player.GetComponent<OrayaMovement>();
        if (potion == null && player != null)
            potion = player.GetComponent<HealingPotion>();

        restPosition = transform.position;
        phase = transform.position.x * 0.37f + transform.position.z * 0.21f;
        restCaptured = true;
    }

    void OnEnable() => ArenaPickups.Register(this);

    void OnDisable()
    {
        ArenaPickups.Unregister(this);
        if (restCaptured)
            transform.position = restPosition;
    }

    void Update()
    {
        if (collected)
            return;

        float wave = Mathf.Sin(Time.time * bobSpeed + phase);
        float bob = (wave * 0.5f + 0.5f) * bobHeight;
        transform.position = restPosition + Vector3.up * bob;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (CollectPressed() && ArenaPickups.IsClosest(this))
            TryCollect();
    }

    public bool TryCollect()
    {
        if (!InRange || !ArenaPickups.IsClosest(this) || potion == null)
            return false;

        collected = true;
        potion.AddCharge();
        gameObject.SetActive(false);
        return true;
    }

    bool GameplayAllowed()
    {
        return movement == null || (movement.InputEnabled && !movement.CinematicMoveActive);
    }

    static bool CollectPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
