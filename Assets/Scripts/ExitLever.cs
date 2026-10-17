using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pulls the exit lever and opens the existing exit gate.
/// Visible and unlocked until a later win condition hides or locks it.
/// </summary>
public class ExitLever : MonoBehaviour, IArenaPickup
{
    [SerializeField] ArenaGate gate;
    [SerializeField] Animator leverAnimator;
    [SerializeField] Transform player;
    [SerializeField] OrayaMovement movement;
    [SerializeField, Min(0.2f)] float range = 2.2f;

    [Header("Availability")]
    [Tooltip("Turn this off later to hide the lever until the trial is won.")]
    [SerializeField] bool leverVisible = true;
    [Tooltip("Turn this off later so the lever cannot be pulled until the trial is won.")]
    [SerializeField] bool leverUnlocked = true;

    bool activated;

    public bool LeverVisible => leverVisible;
    public bool LeverUnlocked => leverUnlocked;
    public bool IsActivated => activated;
    public string Prompt => "Press E to Open Exit Gate";

    public bool InRange
    {
        get
        {
            if (activated || !leverVisible || !leverUnlocked || player == null || !GameplayAllowed())
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
        if (leverAnimator == null)
            leverAnimator = GetComponentInChildren<Animator>();

        ApplyAvailability();
    }

    void OnEnable()
    {
        ApplyAvailability();
        if (leverVisible && !activated)
            ArenaPickups.Register(this);
    }

    void OnDisable()
    {
        ArenaPickups.Unregister(this);
    }

    void Update()
    {
        if (activated || !leverVisible || !leverUnlocked)
            return;
        if (CollectPressed() && ArenaPickups.IsClosest(this))
            TryActivate();
    }

    /// <summary>
    /// Later the win condition can reveal and unlock the lever without changing this component.
    /// </summary>
    public void SetAvailability(bool visible, bool unlocked)
    {
        leverVisible = visible;
        leverUnlocked = unlocked;
        ApplyAvailability();
    }

    public bool TryActivate()
    {
        if (activated || !leverVisible || !leverUnlocked)
            return false;
        if (!InRange || !ArenaPickups.IsClosest(this))
            return false;

        activated = true;
        ArenaPickups.Unregister(this);

        var reach = player != null ? player.GetComponent<OrayaLeverReach>() : null;
        if (reach != null && leverAnimator != null)
            reach.Begin(leverAnimator.transform, Pull);
        else
            Pull();
        return true;
    }

    void Pull()
    {
        if (leverAnimator != null)
            leverAnimator.SetBool("LeverUp", true);
        if (gate != null && !gate.IsOpen)
            gate.Open();
    }

    void ApplyAvailability()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = leverVisible;
        }

        if (!leverVisible || activated)
            ArenaPickups.Unregister(this);
        else if (isActiveAndEnabled)
            ArenaPickups.Register(this);
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
