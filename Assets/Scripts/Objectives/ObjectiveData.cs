using System;
using UnityEngine;

/// <summary>
/// One objective, authored as data. Create assets from the Kejari/Objective menu.
/// Levels share these assets. <see cref="ObjectiveManager"/> decides which one is current.
/// </summary>
[CreateAssetMenu(fileName = "Objective", menuName = "Kejari/Objective")]
public class ObjectiveData : ScriptableObject
{
    [Tooltip("Stable id. Completion is remembered by this id across scene loads.")]
    public string objectiveId;

    public string title;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("All of these StoryStateManager flags must be true before this objective can become current.")]
    public string[] requiredFlags = Array.Empty<string>();

    [Tooltip("When all of these flags are true, the objective completes on its own. Leave empty to complete it from code.")]
    public string[] completeWhenFlags = Array.Empty<string>();
}
