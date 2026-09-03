using System.Collections.Generic;
using UnityEngine;

// One consequence. "gain 10 gold", "lose 5 hp", "remember that you spared the wolf".
//
// Like Requirement, you add these in the Inspector rather than writing code.

[System.Serializable]
public class Effect
{
    public enum Kind
    {
        SetFlag,    // turn a flag on
        ClearFlag,  // turn a flag off
        AddToStat,  // add value to a number - use a NEGATIVE value to subtract
        SetStat,    // force a number to exactly value
    }

    [Tooltip("What this does.")]
    public Kind kind = Kind.AddToStat;

    [Tooltip("The flag or number to change.")]
    public string key = "";

    [Tooltip("How much. Ignored by SetFlag and ClearFlag. " +
             "Use a negative number with AddToStat to take something away.")]
    public int value = 0;

    public void ApplyTo(AdventureState state)
    {
        if (state == null) return;

        switch (kind)
        {
            case Kind.SetFlag:   state.SetFlag(key);          break;
            case Kind.ClearFlag: state.ClearFlag(key);        break;
            case Kind.AddToStat: state.AddToStat(key, value); break;
            case Kind.SetStat:   state.SetStat(key, value);   break;
        }
    }

    // A short line for the UI, so an option can show its price up front.
    // For example: "gold -10".
    public string Describe()
    {
        switch (kind)
        {
            case Kind.SetFlag:   return "gain " + key;
            case Kind.ClearFlag: return "lose " + key;
            case Kind.SetStat:   return key + " becomes " + value;

            case Kind.AddToStat:
                if (value >= 0) return key + " +" + value;
                return key + " " + value; // the minus sign is already there
        }

        return "";
    }

    // Applies a whole list. Copes with a missing list and empty Inspector slots.
    public static void ApplyAll(List<Effect> effects, AdventureState state)
    {
        if (effects == null) return;

        foreach (Effect effect in effects)
        {
            if (effect == null) continue;

            effect.ApplyTo(state);
        }
    }

    // "gold -10, gain muddy_boots" - for showing an option's price on the button.
    public static string DescribeAll(List<Effect> effects)
    {
        if (effects == null) return "";

        List<string> parts = new List<string>();

        foreach (Effect effect in effects)
        {
            if (effect == null) continue;

            string line = effect.Describe();
            if (line != "") parts.Add(line);
        }

        return string.Join(", ", parts.ToArray());
    }
}
