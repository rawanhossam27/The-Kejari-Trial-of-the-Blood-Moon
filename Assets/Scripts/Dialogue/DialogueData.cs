using System;
using UnityEngine;

/// <summary>
/// One authored conversation. Create assets from the Kejari/Dialogue menu.
/// Speaker name and portrait are data, so Oraya, Raihn, Vincent, Mische, and later
/// characters do not need their own dialogue scripts.
/// </summary>
[CreateAssetMenu(fileName = "Dialogue", menuName = "Kejari/Dialogue")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Lines in order. A line with choices waits for a response instead of E or Space.")]
    public DialogueLine[] lines = Array.Empty<DialogueLine>();

    [Tooltip("Written to StoryStateManager when the player reaches the end of this conversation.")]
    public DialogueFlagWrite[] onComplete = Array.Empty<DialogueFlagWrite>();
}

/// <summary>One spoken line, plus optional responses.</summary>
[Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite portrait;

    [TextArea(2, 5)]
    public string text;

    [Tooltip("After this line is finished, the conversation ends instead of continuing to the next line.")]
    public bool endsConversation;

    [Tooltip("Leave empty for a normal line. E or Space then continues.")]
    public DialogueChoice[] choices = Array.Empty<DialogueChoice>();
}

/// <summary>
/// A response button. nextLine is the line to show after the click.
/// -1 ends the conversation.
/// </summary>
[Serializable]
public class DialogueChoice
{
    public string text;

    [Tooltip("Line index to show next. -1 ends the conversation.")]
    public int nextLine = -1;

    public DialogueFlagWrite[] flags = Array.Empty<DialogueFlagWrite>();
}

/// <summary>One story flag to write when a choice is taken or a conversation ends.</summary>
[Serializable]
public class DialogueFlagWrite
{
    public string flagId;
    public bool value = true;
}
