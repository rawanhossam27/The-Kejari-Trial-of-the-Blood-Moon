using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Plays a <see cref="DialogueData"/> conversation and writes its flags to <see cref="StoryStateManager"/>.
/// E and Space are read only while a conversation is running, so pickup, lever, and cinematic input keep those keys the rest of the time.
/// </summary>
[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    [SerializeField] DialogueUI ui;
    [SerializeField] OrayaMovement movement;
    [SerializeField] PlayerHealth health;
    [SerializeField] OrayaLeverReach leverReach;

    DialogueData active;
    int lineIndex = -1;
    bool choicesShown;
    bool restoreInput;
    int startedFrame = -1;

    /// <summary>The conversation that just finished. Raised after its completion flags are written.</summary>
    public event Action<DialogueData> ConversationFinished;

    public static DialogueManager Instance { get; private set; }

    public bool IsRunning => active != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DialogueManager: a second manager was ignored.");
            enabled = false;
            return;
        }

        Instance = this;
        if (ui == null)
            ui = GetComponent<DialogueUI>();
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
        if (health == null && movement != null)
            health = movement.GetComponent<PlayerHealth>();
        if (leverReach == null && movement != null)
            leverReach = movement.GetComponent<OrayaLeverReach>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsRunning || ui == null || Time.frameCount == startedFrame)
            return;

        // First press finishes the typewriter. The next press advances the line.
        if (ui.IsRevealing)
        {
            if (AdvancePressed())
                ui.CompleteReveal();
            return;
        }

        if (LineHasChoices(lineIndex) && !choicesShown)
        {
            ShowCurrentChoices();
            return;
        }

        // Choices are buttons. E and Space must not also skip past them.
        if (ui.ChoicesVisible)
            return;

        if (AdvancePressed())
            Advance();
    }

    /// <summary>Starts a conversation. Returns false when one is already running or gameplay is not ready.</summary>
    public bool StartDialogue(DialogueData conversation)
    {
        if (IsRunning || ui == null || conversation == null || conversation.lines == null || conversation.lines.Length == 0)
            return false;
        if (!GameplayReady())
            return false;

        active = conversation;
        lineIndex = 0;
        choicesShown = false;
        // The E that opens the conversation must not also skip the first line.
        startedFrame = Time.frameCount;
        HoldGameplay();
        ShowLine(lineIndex);
        return true;
    }

    /// <summary>
    /// Stops the current conversation without writing completion flags and without raising
    /// <see cref="ConversationFinished"/>. Use this to cancel. Reaching the last line ends it normally.
    /// </summary>
    public void EndDialogue()
    {
        if (!IsRunning)
            return;

        active = null;
        lineIndex = -1;
        choicesShown = false;
        if (ui != null)
            ui.Hide();
        ReleaseGameplay();
    }

    /// <summary>The same advance used by E and Space, once the current line is fully visible.</summary>
    public void TryAdvance()
    {
        if (!IsRunning || ui == null || ui.IsRevealing || ui.ChoicesVisible)
            return;

        Advance();
    }

    void Advance()
    {
        int next = lineIndex + 1;
        if (active.lines[lineIndex].endsConversation || next >= active.lines.Length)
            Complete();
        else
            ShowLine(next);
    }

    void ShowLine(int index)
    {
        if (active.lines == null || index < 0 || index >= active.lines.Length)
        {
            Complete();
            return;
        }

        lineIndex = index;
        choicesShown = false;
        DialogueLine line = active.lines[index];
        ui.ShowLine(line.speakerName, line.portrait, line.text);
    }

    void ShowCurrentChoices()
    {
        DialogueChoice[] choices = active.lines[lineIndex].choices;
        var labels = new string[choices.Length];
        for (int i = 0; i < choices.Length; i++)
            labels[i] = choices[i] != null ? choices[i].text : string.Empty;

        choicesShown = true;
        ui.ShowChoices(labels, Choose);
    }

    void Choose(int index)
    {
        if (!IsRunning || ui == null || ui.IsRevealing)
            return;

        DialogueChoice[] choices = active.lines[lineIndex].choices;
        if (choices == null || index < 0 || index >= choices.Length || choices[index] == null)
            return;

        DialogueChoice choice = choices[index];
        ApplyFlags(choice.flags);
        ui.ClearChoices();
        choicesShown = false;

        // A choice that points at itself would loop forever, so treat that as the end.
        if (choice.nextLine < 0 || choice.nextLine == lineIndex || choice.nextLine >= active.lines.Length)
            Complete();
        else
            ShowLine(choice.nextLine);
    }

    void Complete()
    {
        DialogueData finished = active;
        ApplyFlags(finished != null ? finished.onComplete : null);
        active = null;
        lineIndex = -1;
        choicesShown = false;
        if (ui != null)
            ui.Hide();
        ReleaseGameplay();
        ConversationFinished?.Invoke(finished);
    }

    static void ApplyFlags(DialogueFlagWrite[] writes)
    {
        if (writes == null || writes.Length == 0 || !StoryStateManager.Exists)
            return;

        StoryStateManager state = StoryStateManager.Instance;
        for (int i = 0; i < writes.Length; i++)
        {
            DialogueFlagWrite write = writes[i];
            if (write == null || string.IsNullOrEmpty(write.flagId))
                continue;

            state.SetFlag(write.flagId, write.value);
        }
    }

    bool LineHasChoices(int index)
    {
        if (active.lines == null || index < 0 || index >= active.lines.Length)
            return false;

        DialogueChoice[] choices = active.lines[index].choices;
        return choices != null && choices.Length > 0;
    }

    bool GameplayReady()
    {
        if (health != null && health.IsDead)
            return false;
        if (leverReach != null && leverReach.IsReaching)
            return false;
        if (movement == null)
            return true;

        // The entry cinematic and other scripted motion own Oraya until they hand control back.
        return movement.InputEnabled && !movement.CinematicMoveActive;
    }

    void HoldGameplay()
    {
        restoreInput = false;
        if (movement == null || !movement.InputEnabled)
            return;

        // Pickups, the lever, attacks, and healing all ignore input while this is false.
        // That keeps E from collecting an item on the same press that advances a line.
        movement.InputEnabled = false;
        restoreInput = true;
    }

    void ReleaseGameplay()
    {
        if (!restoreInput || movement == null)
        {
            restoreInput = false;
            return;
        }

        restoreInput = false;
        if (health != null && health.IsDead)
            return;
        if (movement.CinematicMoveActive)
            return;

        movement.InputEnabled = true;
    }

    static bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
