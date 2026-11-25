using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the demons of this trial. Normal demons register themselves.
/// Only the Alpha's death defeats the rest and completes the trial.
/// </summary>
[DisallowMultipleComponent]
public class BloodMoonTrial : MonoBehaviour
{
    readonly List<TrialDemon> members = new List<TrialDemon>();

    bool victoryResolved;

    public static BloodMoonTrial Instance { get; private set; }

    public bool VictoryResolved => victoryResolved;

    public event Action Victory;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("BloodMoonTrial: a second trial was ignored.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static BloodMoonTrial Ensure()
    {
        if (Instance != null)
            return Instance;

        var existing = FindAnyObjectByType<BloodMoonTrial>();
        if (existing != null)
            return existing;

        var host = new GameObject("BloodMoonTrial");
        return host.AddComponent<BloodMoonTrial>();
    }

    public void Register(TrialDemon demon)
    {
        if (demon == null || members.Contains(demon))
            return;

        members.Add(demon);
    }

    public void Unregister(TrialDemon demon)
    {
        members.Remove(demon);
    }

    public void ResolveAlphaDeath(TrialDemon alpha)
    {
        if (victoryResolved || alpha == null)
            return;

        victoryResolved = true;
        var snapshot = members.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            TrialDemon demon = snapshot[i];
            if (demon == null || demon == alpha || demon.Role != TrialDemonRole.Normal)
                continue;

            demon.DefeatFromTrial();
        }

        if (StoryStateManager.Exists)
        {
            StoryStateManager story = StoryStateManager.Instance;
            if (!story.IsFlagSet(StoryStateManager.Flags.AlphaIdentified))
                story.SetFlag(StoryStateManager.Flags.AlphaIdentified, true);
            story.SetFlag(StoryStateManager.Flags.AlphaDefeated, true);
            story.SetFlag(StoryStateManager.Flags.TrialCompleted, true);
        }

        Victory?.Invoke();
    }
}
