using System.Collections.Generic;
using UnityEngine;

// One condition. "needs 10 gold", "must have the rusty key",
// "must NOT have met the hermit yet".
//
// You never write code for these - you add them in the Inspector: pick a Kind
// from the dropdown, type a key, set a number if the kind uses one.

[System.Serializable]
public class Requirement
{
    public enum Kind
    {
        HasFlag,          // key is a flag name, like "has_rusty_key"
        DoesNotHaveFlag,  // the opposite
        StatAtLeast,      // key is a number name: gold >= value
        StatAtMost,       // gold <= value
    }

    [Tooltip("What kind of check this is.")]
    public Kind kind = Kind.HasFlag;

    [Tooltip("The flag or number to check. Pick a spelling and stick to it - " +
             "\"gold\" and \"Gold\" are two completely different things.")]
    public string key = "";

    [Tooltip("Only used by the StatAtLeast and StatAtMost kinds.")]
    public int value = 0;

    public bool IsMet(AdventureState state)
    {
        if (state == null) return false;

        switch (kind)
        {
            case Kind.HasFlag:         return state.HasFlag(key);
            case Kind.DoesNotHaveFlag: return !state.HasFlag(key);
            case Kind.StatAtLeast:     return state.GetStat(key) >= value;
            case Kind.StatAtMost:      return state.GetStat(key) <= value;
        }

        return true; // unknown kind - never block the player because of a bug here
    }

    // A short line for the UI, so a greyed-out option can say WHY.
    // For example: "Needs gold 10 or more".
    public string Describe()
    {
        switch (kind)
        {
            case Kind.HasFlag:         return "Needs " + key;
            case Kind.DoesNotHaveFlag: return "Only without " + key;
            case Kind.StatAtLeast:     return "Needs " + key + " " + value + " or more";
            case Kind.StatAtMost:      return "Needs " + key + " " + value + " or less";
        }

        return "";
    }

    // Shortcut used all over the kit: are ALL of these met?
    // An empty or missing list counts as "yes", which is what you want -
    // a choice with no requirements is always available.
    public static bool AllMet(List<Requirement> requirements, AdventureState state)
    {
        if (requirements == null) return true;

        foreach (Requirement requirement in requirements)
        {
            if (requirement == null) continue; // empty slot in the Inspector

            if (!requirement.IsMet(state)) return false;
        }

        return true;
    }
}
