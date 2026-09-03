using System.Collections.Generic;
using UnityEngine;

// One "page" of the adventure: some text to read, and the options to pick from.
//
// A random event is the exact same thing. The only difference is how you got to
// it: a story page is reached from another page's choice, a random event is
// pulled out of an EventPool. Same asset type either way, so you only ever
// learn one thing.
//
// An event with an EMPTY choices list is an ending.

[CreateAssetMenu(menuName = "Adventure/Event")]
public class AdventureEvent : ScriptableObject
{
    [Tooltip("Heading shown to the player. If empty, the asset's file name is used.")]
    public string title = "";

    [TextArea(3, 12)]
    [Tooltip("The body text the player reads.")]
    public string bodyText = "";

    [Tooltip("Happens the moment this event appears, BEFORE the player picks anything. " +
             "Perfect for random events: \"you stumble across 10 gold\". " +
             "Runs again if the player ever returns to this event.")]
    public List<Effect> effectsOnArrival = new List<Effect>();

    [Tooltip("The options the player picks from. Leave empty to make this an ending.")]
    public List<Choice> choices = new List<Choice>();

    // Same permanent hidden ID as the node graph kit, for the same reason:
    // save files store this, so you can rename and move your assets freely.
    [SerializeField, HideInInspector]
    private string id;

    public string GetTitle()
    {
        if (string.IsNullOrEmpty(title))
        {
            return name; // the asset's file name
        }
        return title;
    }

    public bool IsEnding()
    {
        return choices == null || choices.Count == 0;
    }

    public string GetId()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();
        }
        return id;
    }

    // Unity calls this in the editor when the asset is created or edited.
    // That is where new events get their ID.
    //
    // If you subclass AdventureEvent and add your own OnValidate, write it as:
    //
    //     protected override void OnValidate()
    //     {
    //         base.OnValidate();
    //         // ...your own checks
    //     }
    //
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    // Ctrl+D copies the hidden ID too. "Collect Events" on your Adventure Book
    // spots that and points you here.
    [ContextMenu("Assign New Id")]
    private void AssignNewId()
    {
        id = MakeNewId();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log("Gave " + name + " a fresh ID. Any old save file loses this event.");
    }

    private static string MakeNewId()
    {
        return System.Guid.NewGuid().ToString("N");
    }
}
