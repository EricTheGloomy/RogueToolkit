using System.Collections.Generic;

// Runs one adventure: which event we are on, which options are pickable, and
// what happens when the player picks one.
//
// It is a normal class, not a MonoBehaviour, so any script can own one. It does
// the RULES; your UI script does the DRAWING. Keeping those apart is why the
// same kit works for a full-screen story, a little popup, or a Console test.

public class AdventureRunner
{
    // The player's flags, numbers and history. Read and poke at it freely.
    public AdventureState state = new AdventureState();

    // The customTag of the last picked choice, or "" if there wasn't one.
    // Read this straight after Choose() to handle the special cases:
    //
    //     runner.Choose(option);
    //     switch (runner.lastCustomTag)
    //     {
    //         case "SPAWN_BOSS":    spawner.SpawnBoss();   break;
    //         case "PLAY_CUTSCENE": cutscene.Play();       break;
    //     }
    //
    public string lastCustomTag = "";

    private AdventureEvent current;

    public AdventureEvent GetCurrentEvent()
    {
        return current;
    }

    // True once the story has run out - the player picked an option with no
    // "goes to", or landed on an event with no choices.
    public bool IsFinished()
    {
        return current == null;
    }

    // Shows an event. Applies its arrival effects and records that the player
    // has now seen it. Use this to start an adventure, or to fire a random event.
    public void Go(AdventureEvent evt)
    {
        current = evt;
        lastCustomTag = "";

        if (current == null) return;

        state.MarkSeen(current);
        Effect.ApplyAll(current.effectsOnArrival, state);
    }

    // Puts us on an event WITHOUT applying its arrival effects or marking it
    // seen. Only for loading a save file - the player already collected those
    // effects before saving, and re-applying them would hand out the reward
    // twice every time they reload.
    public void ResumeAt(AdventureEvent evt)
    {
        current = evt;
        lastCustomTag = "";
    }

    // The options to actually put on screen.
    //
    // Unavailable options are INCLUDED, so you can draw them greyed out with
    // their requirement text - unless the choice itself says to hide it.
    // Check option.IsAvailable(runner.state) to decide how to draw each one.
    public List<Choice> GetVisibleChoices()
    {
        List<Choice> visible = new List<Choice>();

        if (current == null || current.choices == null) return visible;

        foreach (Choice choice in current.choices)
        {
            if (choice == null) continue; // empty slot in the Inspector

            if (choice.IsAvailable(state))
            {
                visible.Add(choice);
            }
            else if (!choice.hideWhenUnavailable)
            {
                visible.Add(choice); // shown, but IsAvailable() will say false
            }
        }

        return visible;
    }

    // Just the ones the player is actually allowed to click.
    public List<Choice> GetAvailableChoices()
    {
        List<Choice> available = new List<Choice>();

        if (current == null || current.choices == null) return available;

        foreach (Choice choice in current.choices)
        {
            if (choice != null && choice.IsAvailable(state))
            {
                available.Add(choice);
            }
        }

        return available;
    }

    // Picks an option. Returns false and changes nothing if it is not allowed,
    // so you can wire this straight to a button without worrying that a stale
    // or greyed-out button will corrupt the game.
    public bool Choose(Choice choice)
    {
        if (current == null || choice == null) return false;

        // Must be an option from the event we are actually on.
        if (current.choices == null || !current.choices.Contains(choice)) return false;

        if (!choice.IsAvailable(state)) return false;

        Effect.ApplyAll(choice.effects, state);

        AdventureEvent next = choice.goesTo;
        Go(next); // a null "goes to" ends the adventure

        // Set this AFTER Go, because Go clears it.
        lastCustomTag = choice.customTag;

        return true;
    }

    // Convenience for keyboard input or a Console test: pick by position in
    // the list that GetVisibleChoices() gave you.
    public bool ChooseVisible(int index)
    {
        List<Choice> visible = GetVisibleChoices();

        if (index < 0 || index >= visible.Count) return false;

        return Choose(visible[index]);
    }

    // Builds the save data, including which event we are on.
    public AdventureSave Save()
    {
        AdventureSave save = state.Save();

        save.currentEventId = (current != null) ? current.GetId() : "";

        return save;
    }
}
