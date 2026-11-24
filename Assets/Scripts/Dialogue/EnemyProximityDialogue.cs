using UnityEngine;

/// <summary>
/// Plays one conversation when Oraya comes within range. No key press, no combat, and no second play.
/// Any later enemy can use the same component with its own <see cref="DialogueData"/>.
/// </summary>
public class EnemyProximityDialogue : MonoBehaviour
{
    [SerializeField] DialogueData conversation;
    [SerializeField] Transform player;
    [SerializeField] OrayaMovement movement;
    [SerializeField, Min(0.2f)] float radius = 2.6f;
    [SerializeField, Min(1f)] float turnDegreesPerSecond = 140f;
    [SerializeField, Min(0.5f)] float readableSeconds = 3.2f;

    bool played;
    bool inside;

    public bool HasPlayed => played;
    public bool IsInsideRadius => inside;

    void Awake()
    {
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
        if (player == null && movement != null)
            player = movement.transform;
    }

    void Update()
    {
        if (player == null)
            return;

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        inside = offset.sqrMagnitude <= radius * radius;

        if (inside && offset.sqrMagnitude > 0.0001f)
            Face(offset);

        if (played || !inside || !CanStart())
            return;

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null || !dialogue.StartDialogue(conversation, false, readableSeconds))
            return;

        played = true;
    }

    void Face(Vector3 flatOffset)
    {
        Quaternion look = Quaternion.LookRotation(flatOffset.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnDegreesPerSecond * Time.deltaTime);
    }

    bool CanStart()
    {
        if (conversation == null || conversation.lines == null || conversation.lines.Length == 0)
            return false;

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue != null && dialogue.IsRunning)
            return false;

        if (movement == null)
            return true;

        return movement.InputEnabled && !movement.CinematicMoveActive;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.72f, 0.18f, 0.22f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
