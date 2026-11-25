using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// The Kejari notice on the vanity. E picks it up once.
/// </summary>
[DisallowMultipleComponent]
public class VanityLetter : MonoBehaviour, IArenaPickup
{
    [SerializeField] RecoveryRoomFlow flow;
    [SerializeField] OrayaMovement movement;
    [SerializeField, Min(0.2f)] float radius = 1.7f;

    float distanceSqr = float.MaxValue;
    bool inside;

    public string Prompt => "E - Pick Up Letter";
    public bool InRange => inside && CanUse();
    public float DistanceSqr => distanceSqr;

    void Awake()
    {
        if (flow == null)
            flow = FindAnyObjectByType<RecoveryRoomFlow>();
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
    }

    void OnEnable()
    {
        ArenaPickups.Register(this);
    }

    void OnDisable()
    {
        ArenaPickups.Unregister(this);
    }

    void Update()
    {
        Refresh();
        if (Pressed() && ArenaPickups.IsClosest(this))
            flow.BeginLetter();
    }

    void Refresh()
    {
        if (movement == null)
        {
            distanceSqr = float.MaxValue;
            inside = false;
            return;
        }

        Vector3 offset = movement.transform.position - transform.position;
        offset.y = 0f;
        distanceSqr = offset.sqrMagnitude;
        inside = distanceSqr <= radius * radius;
    }

    bool CanUse()
    {
        if (flow == null || movement == null)
            return false;
        if (!movement.InputEnabled || movement.CinematicMoveActive)
            return false;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning)
            return false;
        if (!StoryStateManager.Exists)
            return false;

        StoryStateManager story = StoryStateManager.Instance;
        return story.IsFlagSet(StoryStateManager.Flags.OrayaGotUp)
            && !story.IsFlagSet(StoryStateManager.Flags.LetterRead)
            && !flow.IsInspectingLetter;
    }

    static bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}

/// <summary>
/// The bed offers sleep only after Raihn has spoken about the next trial.
/// </summary>
[DisallowMultipleComponent]
public class BedSleep : MonoBehaviour, IArenaPickup
{
    [SerializeField] RecoveryRoomFlow flow;
    [SerializeField] OrayaMovement movement;
    [SerializeField, Min(0.2f)] float radius = 1.8f;

    float distanceSqr = float.MaxValue;
    bool inside;

    public string Prompt => "E - Sleep";
    public bool InRange => inside && CanUse();
    public float DistanceSqr => distanceSqr;

    void Awake()
    {
        if (flow == null)
            flow = FindAnyObjectByType<RecoveryRoomFlow>();
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
    }

    void OnEnable()
    {
        ArenaPickups.Register(this);
    }

    void OnDisable()
    {
        ArenaPickups.Unregister(this);
    }

    void Update()
    {
        Refresh();
        if (Pressed() && ArenaPickups.IsClosest(this))
            flow.BeginSleep();
    }

    void Refresh()
    {
        if (movement == null)
        {
            distanceSqr = float.MaxValue;
            inside = false;
            return;
        }

        Vector3 offset = movement.transform.position - transform.position;
        offset.y = 0f;
        distanceSqr = offset.sqrMagnitude;
        inside = distanceSqr <= radius * radius;
    }

    bool CanUse()
    {
        if (flow == null || flow.IsBusy || movement == null)
            return false;
        if (!movement.InputEnabled || movement.CinematicMoveActive)
            return false;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning)
            return false;
        if (!StoryStateManager.Exists)
            return false;

        StoryStateManager story = StoryStateManager.Instance;
        return story.IsFlagSet(StoryStateManager.Flags.RaihnTrialSpoken)
            && !story.IsFlagSet(StoryStateManager.Flags.RecoveryCompleted);
    }

    static bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
