using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single store of story flags for The Kejari. Put it on its own active GameObject.
/// That object is kept with <see cref="Object.DontDestroyOnLoad"/>, so the flags survive scene changes.
/// Other scripts reach it through <see cref="Instance"/>. Nothing in this class searches the scene by name,
/// reads input, loads scenes, or sets a flag on its own.
/// </summary>
/// <remarks>
/// The flags line up with the three stages, but only later narrative scripts should set them:
/// Moon Palace (orayaIntroduced, kejariExplained, raihnEncountered),
/// Blood Moon Arena (trialStarted, alphaIdentified, alphaDefeated, trialCompleted),
/// Recovery Area (allianceDecisionMade, allianceAccepted), plus nextStageUnlocked for the stage gate.
/// </remarks>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class StoryStateManager : MonoBehaviour
{
    /// <summary>Stable flag ids. Compare with these constants so a typo cannot create a second flag.</summary>
    public static class Flags
    {
        public const string OrayaIntroduced = "orayaIntroduced";
        public const string KejariExplained = "kejariExplained";
        public const string RaihnEncountered = "raihnEncountered";
        public const string TrialStarted = "trialStarted";
        public const string AlphaIdentified = "alphaIdentified";
        public const string AlphaDefeated = "alphaDefeated";
        public const string TrialCompleted = "trialCompleted";
        public const string AllianceDecisionMade = "allianceDecisionMade";
        public const string AllianceAccepted = "allianceAccepted";
        public const string NextStageUnlocked = "nextStageUnlocked";
    }

    static readonly string[] InitialFlags =
    {
        Flags.OrayaIntroduced,
        Flags.KejariExplained,
        Flags.RaihnEncountered,
        Flags.TrialStarted,
        Flags.AlphaIdentified,
        Flags.AlphaDefeated,
        Flags.TrialCompleted,
        Flags.AllianceDecisionMade,
        Flags.AllianceAccepted,
        Flags.NextStageUnlocked
    };

    static StoryStateManager instance;

    // Runtime values live on the persistent instance only. A copy loaded with the next scene has its own empty map.
    readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();

    /// <summary>
    /// Flag id, then the value it changed to. Raised only when a flag's stored value actually changes.
    /// </summary>
    public event Action<string, bool> FlagChanged;

    /// <summary>
    /// The live manager, or null when play mode has no instance yet.
    /// Callers must tolerate null so a scene still runs before this object is placed.
    /// </summary>
    public static StoryStateManager Instance => instance;

    /// <summary>True when a manager has claimed the singleton during this play session.</summary>
    public static bool Exists => instance != null;

    void Awake()
    {
        // A manager from an earlier scene is already alive. This copy came in with the scene
        // that just loaded (or with a scene reload). Throw the copy away.
        // Never copy its flags over: a freshly loaded object would reset the story to all-false.
        if (instance != null && instance != this)
        {
            // Two components on one object: destroy only the extra component.
            // Destroying the GameObject would also destroy the instance we are keeping.
            if (instance.gameObject == gameObject)
                Destroy(this);
            else
                Destroy(gameObject);
            return;
        }

        instance = this;
        PersistAcrossScenes();
        SeedInitialFlags();
    }

    void OnDestroy()
    {
        // The duplicate's OnDestroy must not clear the real instance.
        if (instance == this)
            instance = null;
    }

    /// <summary>
    /// Stores <paramref name="value"/> for <paramref name="flagId"/>.
    /// Unknown ids are accepted, so later stages can add flags without changing this class.
    /// Ids are case-sensitive. Setting a flag to the value it already has does nothing and raises no event.
    /// </summary>
    public void SetFlag(string flagId, bool value)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            Debug.LogWarning("StoryStateManager: ignored a flag with an empty id.");
            return;
        }

        // A missing flag reads as false, so writing false for the first time is not a change.
        flags.TryGetValue(flagId, out bool current);
        if (current == value)
            return;

        flags[flagId] = value;
        FlagChanged?.Invoke(flagId, value);
    }

    /// <summary>The stored value, or false when the id is empty or has never been set.</summary>
    public bool GetFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
            return false;

        return flags.TryGetValue(flagId, out bool value) && value;
    }

    /// <summary>True when the flag is currently set. An unknown id is not set.</summary>
    public bool IsFlagSet(string flagId)
    {
        return GetFlag(flagId);
    }

    /// <summary>
    /// New-game reset. Every flag that was true becomes false, and each of those changes raises
    /// <see cref="FlagChanged"/>. Flags added during the run are removed. The manager itself stays alive.
    /// Safe to call more than once.
    /// </summary>
    public void ResetStory()
    {
        // Snapshot the keys first. SetFlag notifies listeners, and a listener may write flags while we iterate.
        var ids = new List<string>(flags.Keys);
        for (int i = 0; i < ids.Count; i++)
            SetFlag(ids[i], false);

        for (int i = 0; i < ids.Count; i++)
        {
            if (!IsInitialFlag(ids[i]))
                flags.Remove(ids[i]);
        }

        SeedInitialFlags();
    }

    void PersistAcrossScenes()
    {
        // DontDestroyOnLoad applies to this GameObject, not to its parent.
        // Detach first so unloading the parent's scene cannot destroy the manager with it.
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
    }

    void SeedInitialFlags()
    {
        // Present from the start, all false, and without a change event.
        // The story systems are what turn them on.
        for (int i = 0; i < InitialFlags.Length; i++)
        {
            if (!flags.ContainsKey(InitialFlags[i]))
                flags.Add(InitialFlags[i], false);
        }
    }

    static bool IsInitialFlag(string flagId)
    {
        for (int i = 0; i < InitialFlags.Length; i++)
        {
            if (InitialFlags[i] == flagId)
                return true;
        }

        return false;
    }
}
