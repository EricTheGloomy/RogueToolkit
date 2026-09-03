using System.Collections.Generic;
using UnityEngine;

// The asset your game points at: one book per story or per set of events.
//
// It knows every event, which is what makes loading a save possible - a save
// file stores an ID, and something has to turn that back into an asset.

[CreateAssetMenu(menuName = "Adventure/Book")]
public class AdventureBook : ScriptableObject
{
    [Tooltip("Where a new adventure begins. Leave empty if this book is only a " +
             "container for random events.")]
    public AdventureEvent firstEvent;

    [Tooltip("Extra events that are NOT reachable from firstEvent - usually the " +
             "ones you only ever pull out of an EventPool. Drag them in here so " +
             "Collect Events can find them.")]
    public List<AdventureEvent> alsoInclude = new List<AdventureEvent>();

    [Tooltip("Every event in this book. Filled in for you - right-click this " +
             "asset and pick 'Collect Events'.")]
    public List<AdventureEvent> allEvents = new List<AdventureEvent>();

    // Starts a brand new adventure at firstEvent.
    public AdventureRunner StartNewAdventure()
    {
        AdventureRunner runner = new AdventureRunner();
        runner.Go(firstEvent);
        return runner;
    }

    // Puts the player back where they left off.
    public AdventureRunner Resume(AdventureSave save)
    {
        AdventureRunner runner = new AdventureRunner();

        runner.state.Load(save);

        if (save == null) return runner;

        AdventureEvent where = FindEventById(save.currentEventId);

        // ResumeAt, not Go - see the comment on ResumeAt. Go would hand out this
        // event's arrival effects a second time.
        runner.ResumeAt(where);

        return runner;
    }

    // Turns a saved ID back into a real event. Returns null if that event was
    // deleted since the save was written, which is not an error - just means
    // the player has to start that bit again.
    public AdventureEvent FindEventById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (AdventureEvent evt in allEvents)
        {
            if (evt != null && evt.GetId() == id)
            {
                return evt;
            }
        }

        return null;
    }

    // Handy for debug commands. Not used by saving, because names can change.
    public AdventureEvent FindEventByName(string eventName)
    {
        foreach (AdventureEvent evt in allEvents)
        {
            if (evt != null && evt.name == eventName)
            {
                return evt;
            }
        }

        return null;
    }

    // Right-click this asset in the Project window and choose "Collect Events".
    // It starts at firstEvent (plus anything in alsoInclude), follows every
    // choice, and fills in allEvents. Re-run it whenever you add events.
    [ContextMenu("Collect Events")]
    public void CollectEvents()
    {
        allEvents.Clear();

        // A to-do list of events we still have to look at.
        List<AdventureEvent> toCheck = new List<AdventureEvent>();

        if (firstEvent != null) toCheck.Add(firstEvent);

        foreach (AdventureEvent extra in alsoInclude)
        {
            if (extra != null) toCheck.Add(extra);
        }

        while (toCheck.Count > 0)
        {
            // Take the first one off the to-do list.
            AdventureEvent current = toCheck[0];
            toCheck.RemoveAt(0);

            // Skip if we already did it. This is what stops us looping forever
            // when two events lead to each other.
            if (allEvents.Contains(current)) continue;

            allEvents.Add(current);

            // Queue up everywhere its choices lead.
            if (current.choices == null) continue;

            foreach (Choice choice in current.choices)
            {
                if (choice != null && choice.goesTo != null)
                {
                    toCheck.Add(choice.goesTo);
                }
            }
        }

        Debug.Log("Collect Events: found " + allEvents.Count + " events in " + name);

        CheckForDuplicateIds();

#if UNITY_EDITOR
        // Tells Unity "this asset changed, please write it to disk".
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // Duplicating an event with Ctrl+D copies its hidden ID as well, so two
    // events look identical to your save file. That is the one thing that breaks
    // ID-based saving, so we check for it and say exactly what to do.
    private void CheckForDuplicateIds()
    {
        for (int i = 0; i < allEvents.Count; i++)
        {
            for (int j = i + 1; j < allEvents.Count; j++)
            {
                if (allEvents[i].GetId() == allEvents[j].GetId())
                {
                    Debug.LogError(
                        "Two events share the same hidden ID: '" + allEvents[i].name +
                        "' and '" + allEvents[j].name + "'. This happens when you " +
                        "duplicate an event with Ctrl+D. Fix it by right-clicking '" +
                        allEvents[j].name + "' and choosing 'Assign New Id'.");
                }
            }
        }
    }
}
