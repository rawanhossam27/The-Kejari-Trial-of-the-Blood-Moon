using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Talks to one NPC. Attach it to any character, assign that character's <see cref="DialogueData"/>,
/// and E starts the conversation through <see cref="DialogueManager"/>.
/// Raihn, Vincent, Mische, and later NPCs use this same component.
/// The NPC joins <see cref="ArenaPickups"/>, so the closest pickup, lever, or NPC is the only one that receives E.
/// The existing bottom prompt shows <see cref="Prompt"/> while this NPC is that closest interaction.
/// </summary>
[DefaultExecutionOrder(-40)]
public class NPCInteraction : MonoBehaviour, IArenaPickup
{
    [SerializeField] DialogueData conversation;
    [SerializeField] Transform player;
    [SerializeField] OrayaMovement movement;
    [SerializeField] PlayerHealth health;
    [SerializeField] OrayaLeverReach leverReach;
    [SerializeField, Min(0.2f)] float radius = 2.5f;
    [SerializeField] string prompt = "E - Talk";

    float distanceSqr = float.MaxValue;
    bool inside;
    bool subscribed;
    int ignoreTalkFrame = -1;

    /// <summary>Oraya crossed into the radius. This is distance only, not whether she is allowed to talk.</summary>
    public event Action EnteredRadius;

    /// <summary>Oraya left the radius.</summary>
    public event Action LeftRadius;

    public DialogueData Conversation => conversation;
    public bool IsInsideRadius => inside;
    public string Prompt => string.IsNullOrEmpty(prompt) ? "E - Talk" : prompt;

    /// <summary>
    /// True when she is inside the radius and talking is allowed.
    /// Pickups and the lever use this to decide who owns E.
    /// </summary>
    public bool InRange => inside && CanTalk();

    public float DistanceSqr => distanceSqr;

    void Awake()
    {
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
        if (player == null && movement != null)
            player = movement.transform;
        if (health == null && movement != null)
            health = movement.GetComponent<PlayerHealth>();
        if (leverReach == null && movement != null)
            leverReach = movement.GetComponent<OrayaLeverReach>();
    }

    void OnEnable()
    {
        ArenaPickups.Register(this);
    }

    void OnDisable()
    {
        ArenaPickups.Unregister(this);
        if (subscribed && DialogueManager.Instance != null)
            DialogueManager.Instance.ConversationFinished -= HandleConversationFinished;
        subscribed = false;
        if (inside)
        {
            inside = false;
            LeftRadius?.Invoke();
        }
    }

    void Update()
    {
        SubscribeWhenReady();
        RefreshProximity();

        // Runs before pickups and the lever, so a closer one still owns this E.
        // Also ignores the press that just finished a conversation.
        if (TalkPressed() && Time.frameCount != ignoreTalkFrame && ArenaPickups.IsClosest(this))
            TryTalk();
    }

    void SubscribeWhenReady()
    {
        if (subscribed || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.ConversationFinished += HandleConversationFinished;
        subscribed = true;
    }

    void HandleConversationFinished(DialogueData _)
    {
        ignoreTalkFrame = Time.frameCount;
    }

    /// <summary>Starts this NPC's conversation. Returns false when she cannot talk, or another interaction is closer.</summary>
    public bool TryTalk()
    {
        if (!ArenaPickups.IsClosest(this) || conversation == null)
            return false;

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null)
            return false;

        return dialogue.StartDialogue(conversation);
    }

    void RefreshProximity()
    {
        if (player == null)
        {
            distanceSqr = float.MaxValue;
            SetInside(false);
            return;
        }

        // Ignore height so a tall capsule does not shrink the talk radius.
        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        distanceSqr = offset.sqrMagnitude;
        SetInside(distanceSqr <= radius * radius);
    }

    void SetInside(bool nowInside)
    {
        if (nowInside == inside)
            return;

        inside = nowInside;
        if (inside)
            EnteredRadius?.Invoke();
        else
            LeftRadius?.Invoke();
    }

    bool CanTalk()
    {
        if (conversation == null)
            return false;
        if (health != null && health.IsDead)
            return false;
        if (leverReach != null && leverReach.IsReaching)
            return false;

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue != null && dialogue.IsRunning)
            return false;

        // False during the entry cinematic and while a conversation has locked movement.
        if (movement != null && (!movement.InputEnabled || movement.CinematicMoveActive))
            return false;

        return true;
    }

    static bool TalkPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.86f, 0.72f, 0.45f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
