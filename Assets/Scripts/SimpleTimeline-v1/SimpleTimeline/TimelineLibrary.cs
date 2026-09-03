using System.Collections.Generic;
using UnityEngine;

// The asset your game points at. It has two jobs, and they are both dull but
// necessary:
//
//   1. It is the PALETTE - the list of actions the player can choose from, which
//      is what you build your hand of cards or your build menu out of.
//
//   2. It is the LOOKUP - a save file stores IDs, and something has to turn
//      those back into real assets.
//
// Make one per situation: PlayerCards, EnemyAttacks, BuildableThings.

[CreateAssetMenu(menuName = "Timeline/Library")]
public class TimelineLibrary : ScriptableObject
{
    [Tooltip("Every action that can go on a timeline from this library. " +
             "Drag your action assets in here.")]
    public List<TimelineAction> allActions = new List<TimelineAction>();

    [Tooltip("Optional. An authored sequence - an enemy attack pattern, a cutscene, " +
             "a tutorial script. Call BuildPreset() to get a ready-made Timeline. " +
             "Leave empty if the player builds their own.")]
    public List<TimelineAction> presetSequence = new List<TimelineAction>();

    // ---------------- making timelines ----------------

    // An empty timeline for the player to fill in.
    // Pass 0 for either limit to mean "no limit".
    public Timeline BuildEmpty(int maxEntries, float maxTotalDuration)
    {
        Timeline timeline = new Timeline();
        timeline.maxEntries = maxEntries;
        timeline.maxTotalDuration = maxTotalDuration;
        return timeline;
    }

    // A timeline filled in from presetSequence, ready to hand to a runner.
    public Timeline BuildPreset()
    {
        Timeline timeline = new Timeline();

        foreach (TimelineAction action in presetSequence)
        {
            if (action == null) continue; // empty slot in the Inspector

            timeline.Add(action);
        }

        return timeline;
    }

    // ---------------- lookup ----------------

    // Turns a saved ID back into a real action. Returns null if that action was
    // deleted since the save was written, which is not an error - Timeline.Load
    // just skips it.
    public TimelineAction FindActionById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (TimelineAction action in allActions)
        {
            if (action != null && action.GetId() == id)
            {
                return action;
            }
        }

        return null;
    }

    // Handy for debug commands. Not used by saving, because names can change.
    public TimelineAction FindActionByName(string actionName)
    {
        foreach (TimelineAction action in allActions)
        {
            if (action != null && action.name == actionName)
            {
                return action;
            }
        }

        return null;
    }

    // ---------------- editor sanity check ----------------

    // Right-click this asset in the Project window and choose "Check For Problems".
    // Run it after adding or duplicating actions.
    [ContextMenu("Check For Problems")]
    public void CheckForProblems()
    {
        int problems = 0;

        // Empty slots.
        for (int i = 0; i < allActions.Count; i++)
        {
            if (allActions[i] == null)
            {
                Debug.LogWarning("[" + name + "] All Actions has an empty slot at position " + i + ".", this);
                problems++;
            }
        }

        // Duplicated IDs. Ctrl+D copies an action's hidden ID along with
        // everything else, so two actions end up looking identical to a save file.
        for (int i = 0; i < allActions.Count; i++)
        {
            if (allActions[i] == null) continue;

            for (int j = i + 1; j < allActions.Count; j++)
            {
                if (allActions[j] == null) continue;

                if (allActions[i].GetId() == allActions[j].GetId())
                {
                    Debug.LogError(
                        "[" + name + "] '" + allActions[i].name + "' and '" + allActions[j].name +
                        "' share the same hidden ID. This happens when you duplicate an action " +
                        "with Ctrl+D. Fix it by right-clicking '" + allActions[j].name +
                        "' and choosing 'Assign New Id'.", allActions[j]);
                    problems++;
                }
            }
        }

        // Preset actions that are not in the palette. Saving a timeline built
        // from the preset would not be able to load it back.
        for (int i = 0; i < presetSequence.Count; i++)
        {
            TimelineAction action = presetSequence[i];

            if (action == null)
            {
                Debug.LogWarning("[" + name + "] Preset Sequence has an empty slot at position "
                                 + i + ", which will silently do nothing.", this);
                problems++;
                continue;
            }

            if (!allActions.Contains(action))
            {
                Debug.LogWarning(
                    "[" + name + "] '" + action.name + "' is in Preset Sequence but not in " +
                    "All Actions, so a saved timeline could not load it back. Drag it into " +
                    "All Actions too.", action);
                problems++;
            }
        }

        if (problems == 0)
        {
            Debug.Log("[" + name + "] No problems. " + allActions.Count + " actions, "
                      + presetSequence.Count + " in the preset.", this);
        }
    }
}
