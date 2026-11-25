using UnityEngine;

/// <summary>
/// Keeps the Alpha asleep until Oraya has finished meeting Raihn, plays his warning, then wakes the boss once.
/// The meeting objective completes when <see cref="StoryStateManager.Flags.RaihnEncountered"/> is set.
/// The warning is a normal <see cref="DialogueData"/> line, so it uses the existing dialogue UI. No voice clip exists for it.
/// </summary>
[DisallowMultipleComponent]
public class AlphaEncounterDirector : MonoBehaviour
{
    [SerializeField] DialogueData meeting;
    [SerializeField] DialogueData warning;

    bool warningArmed;
    bool warningStarted;
    bool awakened;

    public bool WarningStarted => warningStarted;
    public bool EncounterStarted => awakened;

    void OnEnable()
    {
        Subscribe();
    }

    void Start()
    {
        Subscribe();
    }

    void OnDisable()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.ConversationFinished -= HandleConversationFinished;
        if (ObjectiveManager.Exists)
            ObjectiveManager.Instance.ObjectiveCompleted -= HandleObjectiveCompleted;
    }

    void Subscribe()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ConversationFinished -= HandleConversationFinished;
            DialogueManager.Instance.ConversationFinished += HandleConversationFinished;
        }

        if (ObjectiveManager.Exists)
        {
            ObjectiveManager.Instance.ObjectiveCompleted -= HandleObjectiveCompleted;
            ObjectiveManager.Instance.ObjectiveCompleted += HandleObjectiveCompleted;
        }
    }

    void HandleObjectiveCompleted(ObjectiveData objective)
    {
        if (awakened || warningStarted || objective == null)
            return;

        if (objective.objectiveId != "meetRaihn" && objective.name != "MeetRaihn")
            return;

        // The meeting conversation is still closing when the objective completes.
        // Wait for that dialogue to finish so the warning is not refused as a second conversation.
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning)
        {
            warningArmed = true;
            return;
        }

        BeginWarning();
    }

    void HandleConversationFinished(DialogueData conversation)
    {
        if (awakened)
            return;

        if (conversation == warning)
        {
            WakeAlpha();
            return;
        }

        if (!warningArmed && conversation != meeting)
            return;

        BeginWarning();
    }

    void BeginWarning()
    {
        if (warningStarted || awakened)
            return;

        warningStarted = true;
        warningArmed = false;

        DialogueManager dialogue = DialogueManager.Instance;
        if (warning == null || dialogue == null || !dialogue.StartDialogue(warning))
            WakeAlpha();
    }

    void WakeAlpha()
    {
        if (awakened)
            return;

        awakened = true;
        EnemyCombatant alpha = FindAlpha();
        if (alpha != null)
            alpha.Awaken();
    }

    static EnemyCombatant FindAlpha()
    {
        var demons = FindObjectsByType<TrialDemon>(FindObjectsSortMode.None);
        for (int i = 0; i < demons.Length; i++)
        {
            if (demons[i] != null && demons[i].Role == TrialDemonRole.Alpha)
                return demons[i].GetComponent<EnemyCombatant>();
        }

        return null;
    }
}
