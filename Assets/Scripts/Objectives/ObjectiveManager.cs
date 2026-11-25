using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the current objective and which ones are already finished.
/// The component persists with DontDestroyOnLoad, so progress survives a scene change.
/// Completion and availability read <see cref="StoryStateManager"/> flags. This class does not store a second copy of the story.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class ObjectiveManager : MonoBehaviour
{
    [Tooltip("Played in order. A later objective waits until every earlier one is finished and its own flags are set.")]
    [SerializeField] ObjectiveData[] sequence = Array.Empty<ObjectiveData>();

    readonly HashSet<string> completed = new HashSet<string>();

    ObjectiveData current;

    /// <summary>The objective that just became current.</summary>
    public event Action<ObjectiveData> ObjectiveActivated;

    /// <summary>The objective that just finished. Raised before the next one, if any, is activated.</summary>
    public event Action<ObjectiveData> ObjectiveCompleted;

    /// <summary>The previous current objective, then the one that took its place.</summary>
    public event Action<ObjectiveData, ObjectiveData> ObjectiveReplaced;

    public static ObjectiveManager Instance { get; private set; }

    public static bool Exists => Instance != null;

    public ObjectiveData Current => current;

    public bool IsCurrentComplete => current != null && completed.Contains(IdOf(current));

    public IReadOnlyList<ObjectiveData> Sequence => sequence;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.gameObject == gameObject)
                Destroy(this);
            else
                Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (Instance != this || !StoryStateManager.Exists)
            return;

        StoryStateManager.Instance.FlagChanged += HandleFlagChanged;
    }

    void Start()
    {
        if (Instance != this)
            return;

        // OnEnable can run before StoryStateManager's Awake if this object is created first.
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.FlagChanged -= HandleFlagChanged;
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.FlagChanged += HandleFlagChanged;

        Evaluate();
    }

    void OnDisable()
    {
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.FlagChanged -= HandleFlagChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsCompleted(ObjectiveData objective)
    {
        return objective != null && completed.Contains(IdOf(objective));
    }

    public bool IsCompleted(string objectiveId)
    {
        return !string.IsNullOrEmpty(objectiveId) && completed.Contains(objectiveId);
    }

    /// <summary>True when every required flag is set. An objective with no requirements is always available.</summary>
    public bool PrerequisitesMet(ObjectiveData objective)
    {
        if (objective == null)
            return false;

        return AllSet(objective.requiredFlags);
    }

    /// <summary>
    /// Makes <paramref name="objective"/> current when nothing else is in progress.
    /// Returns false if its flags are not set, it is already finished, or another objective is still active.
    /// </summary>
    public bool Activate(ObjectiveData objective)
    {
        if (Instance != this || objective == null || IsCompleted(objective) || !PrerequisitesMet(objective))
            return false;

        if (current != null && current != objective && !IsCurrentComplete)
            return false;

        if (current == objective)
            return true;

        SetCurrent(objective, current, current != null);
        return current == objective;
    }

    /// <summary>Finishes the current objective, then moves to the next one in the sequence whose flags are set.</summary>
    public bool CompleteCurrent()
    {
        if (Instance != this || current == null || IsCurrentComplete)
            return false;

        completed.Add(IdOf(current));
        ObjectiveData finished = current;
        ObjectiveCompleted?.Invoke(finished);
        Advance(finished);
        return true;
    }

    /// <summary>
    /// Swaps the current objective without marking the previous one finished.
    /// Returns false when <paramref name="next"/> is missing, already finished, or its flags are not set.
    /// </summary>
    public bool Replace(ObjectiveData next)
    {
        if (Instance != this || next == null || IsCompleted(next) || !PrerequisitesMet(next))
            return false;

        if (current == next)
            return true;

        SetCurrent(next, current, true);
        return current == next;
    }

    /// <summary>Clears the current objective and every completion. The next evaluation starts the sequence again.</summary>
    public void ResetProgress()
    {
        if (Instance != this)
            return;

        completed.Clear();
        current = null;
        Evaluate();
    }

    void HandleFlagChanged(string flagId, bool value)
    {
        if (Instance != this)
            return;

        if (!value && !AnyInitialFlagSet())
        {
            completed.Clear();
            current = null;
        }

        Evaluate();
    }

    void Evaluate()
    {
        RewindFlagDrivenCompletions();

        if (current != null && !IsCurrentComplete && !PrerequisitesMet(current))
            current = null;

        if (current != null && !IsCurrentComplete && ReadyToComplete(current))
        {
            CompleteCurrent();
            return;
        }

        if (current == null || IsCurrentComplete)
            Advance(current);
    }

    void Advance(ObjectiveData previous)
    {
        ObjectiveData next = FindNext();
        if (next == null || next == current)
            return;

        SetCurrent(next, previous, previous != null);
    }

    ObjectiveData FindNext()
    {
        if (sequence == null)
            return null;

        for (int i = 0; i < sequence.Length; i++)
        {
            ObjectiveData candidate = sequence[i];
            if (candidate == null || IsCompleted(candidate))
                continue;

            // Do not skip a locked objective and start a later one.
            if (!PrerequisitesMet(candidate))
                return null;

            return candidate;
        }

        return null;
    }

    void SetCurrent(ObjectiveData next, ObjectiveData previous, bool replaced)
    {
        current = next;
        if (replaced && previous != null && previous != next)
            ObjectiveReplaced?.Invoke(previous, next);

        ObjectiveActivated?.Invoke(next);

        if (current == next && ReadyToComplete(next))
            CompleteCurrent();
    }

    void RewindFlagDrivenCompletions()
    {
        if (sequence == null || completed.Count == 0)
            return;

        var finished = new List<string>(completed);
        for (int i = 0; i < finished.Count; i++)
        {
            ObjectiveData data = FindById(finished[i]);
            if (data == null || data.completeWhenFlags == null || data.completeWhenFlags.Length == 0)
                continue;

            if (!AllSet(data.completeWhenFlags))
                completed.Remove(finished[i]);
        }
    }

    ObjectiveData FindById(string objectiveId)
    {
        if (sequence == null || string.IsNullOrEmpty(objectiveId))
            return null;

        for (int i = 0; i < sequence.Length; i++)
        {
            if (sequence[i] != null && IdOf(sequence[i]) == objectiveId)
                return sequence[i];
        }

        return null;
    }

    bool ReadyToComplete(ObjectiveData objective)
    {
        if (objective == null || objective.completeWhenFlags == null || objective.completeWhenFlags.Length == 0)
            return false;

        return AllSet(objective.completeWhenFlags);
    }

    static bool AllSet(string[] flagIds)
    {
        if (flagIds == null || flagIds.Length == 0)
            return true;

        if (!StoryStateManager.Exists)
            return false;

        for (int i = 0; i < flagIds.Length; i++)
        {
            if (string.IsNullOrEmpty(flagIds[i]))
                continue;

            if (!StoryStateManager.Instance.IsFlagSet(flagIds[i]))
                return false;
        }

        return true;
    }

    static bool AnyInitialFlagSet()
    {
        if (!StoryStateManager.Exists)
            return false;

        StoryStateManager story = StoryStateManager.Instance;
        return story.IsFlagSet(StoryStateManager.Flags.OrayaIntroduced)
            || story.IsFlagSet(StoryStateManager.Flags.KejariExplained)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnEncountered)
            || story.IsFlagSet(StoryStateManager.Flags.TrialStarted)
            || story.IsFlagSet(StoryStateManager.Flags.AlphaIdentified)
            || story.IsFlagSet(StoryStateManager.Flags.AlphaDefeated)
            || story.IsFlagSet(StoryStateManager.Flags.TrialCompleted)
            || story.IsFlagSet(StoryStateManager.Flags.RecoveryArrived)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnSpotted)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnPotionAccepted)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnPotionDeclined)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnBedsideSpoken)
            || story.IsFlagSet(StoryStateManager.Flags.OrayaGotUp)
            || story.IsFlagSet(StoryStateManager.Flags.LetterDiscovered)
            || story.IsFlagSet(StoryStateManager.Flags.LetterRead)
            || story.IsFlagSet(StoryStateManager.Flags.RaihnTrialSpoken)
            || story.IsFlagSet(StoryStateManager.Flags.RecoveryCompleted)
            || story.IsFlagSet(StoryStateManager.Flags.AllianceDecisionMade)
            || story.IsFlagSet(StoryStateManager.Flags.AllianceAccepted)
            || story.IsFlagSet(StoryStateManager.Flags.NextStageUnlocked);
    }

    public static string IdOf(ObjectiveData objective)
    {
        if (objective == null)
            return string.Empty;

        return string.IsNullOrEmpty(objective.objectiveId) ? objective.name : objective.objectiveId;
    }
}
