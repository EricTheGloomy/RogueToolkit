using System.Collections.Generic;
using UnityEngine;

// One option the player can pick.
//
// Note this is NOT a ScriptableObject - you do not make an asset per choice.
// Choices are edited inline, right inside their event's Inspector, which is
// much less clicking. That is what [System.Serializable] buys you.

[System.Serializable]
public class Choice
{
    [Tooltip("The button text: \"Pay the toll\", \"Draw your sword\".")]
    public string text = "";

    [Tooltip("ALL of these must be true before the player is allowed to pick this. " +
             "Leave the list empty for an option that is always available.")]
    public List<Requirement> requirements = new List<Requirement>();

    [Tooltip("ON: the option vanishes completely when the player cannot pick it.\n" +
             "OFF: it still shows, greyed out, so the player knows it exists. " +
             "Greyed out is usually the better game design - it teases.")]
    public bool hideWhenUnavailable = false;

    [Tooltip("What happens when the player picks this.")]
    public List<Effect> effects = new List<Effect>();

    [Tooltip("The event to show next. LEAVE EMPTY to end the adventure here.")]
    public AdventureEvent goesTo;

    [Tooltip("Optional. Your own code can react to this with a switch statement, " +
             "for things flags and numbers cannot express: \"SPAWN_BOSS\", " +
             "\"PLAY_CUTSCENE\", \"OPEN_SHOP\". Read runner.lastCustomTag after Choose().")]
    public string customTag = "";

    // Can the player pick this right now?
    public bool IsAvailable(AdventureState state)
    {
        return Requirement.AllMet(requirements, state);
    }

    // "Needs gold 10 or more" - show this next to a greyed-out option.
    public string DescribeRequirements()
    {
        List<string> parts = new List<string>();

        foreach (Requirement requirement in requirements)
        {
            if (requirement == null) continue;

            string line = requirement.Describe();
            if (line != "") parts.Add(line);
        }

        return string.Join(", ", parts.ToArray());
    }

    // "gold -10" - show this on the button so the price is visible before clicking.
    public string DescribeCost()
    {
        return Effect.DescribeAll(effects);
    }
}
