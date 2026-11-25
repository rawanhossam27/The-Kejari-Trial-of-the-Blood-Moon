using UnityEngine;

/// <summary>
/// Loads the next scene only after Oraya walks through an opened exit.
/// Opening the gate sets the story flag. This trigger is the physical step after that.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ExitThreshold : MonoBehaviour
{
    [SerializeField] string sceneName = SceneTransitionController.RecoveryRoomScene;
    [SerializeField] ArenaGate gate;

    bool used;

    public bool HasBeenUsed => used;

    void Awake()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (used || other == null)
            return;

        OrayaMovement movement = other.GetComponentInParent<OrayaMovement>();
        if (movement == null)
            return;

        if (!CanLeave())
            return;

        used = true;
        Lock(movement);
        SceneTransitionController.Ensure().TransitionTo(sceneName);
    }

    /// <summary>
    /// The trial has to be won and the exit doors have to have finished opening.
    /// A missing story manager does not count as permission to leave.
    /// </summary>
    public bool CanLeave()
    {
        if (!StoryStateManager.Exists)
            return false;

        StoryStateManager story = StoryStateManager.Instance;
        if (!story.IsFlagSet(StoryStateManager.Flags.AlphaDefeated))
            return false;
        if (!story.IsFlagSet(StoryStateManager.Flags.TrialCompleted))
            return false;
        if (!story.IsFlagSet(StoryStateManager.Flags.NextStageUnlocked))
            return false;
        if (gate != null && !gate.IsOpen)
            return false;

        return true;
    }

    static void Lock(OrayaMovement movement)
    {
        movement.EndCinematicMove();
        movement.InputEnabled = false;
        movement.enabled = false;

        CharacterController body = movement.GetComponent<CharacterController>();
        if (body != null)
            body.enabled = false;

        OrayaCameraManager cameras = movement.GetComponent<OrayaCameraManager>();
        if (cameras != null)
            cameras.BeginEntryCinematic(null);
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null)
            return;

        Gizmos.color = new Color(0.45f, 0.75f, 1f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
