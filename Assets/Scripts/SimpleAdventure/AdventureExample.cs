using System.Collections.Generic;
using UnityEngine;

// A throwaway demo. Put it on any GameObject and press Play.
//
// Leave the "book" slot EMPTY and it builds a tiny adventure in code, so you can
// watch the whole system work with zero setup. Drop your own Adventure Book in
// and it runs that instead.
//
// Delete this file once things click. Nothing else needs it.

public class AdventureExample : MonoBehaviour
{
    [Tooltip("Optional. Leave empty to run the built-in demo.")]
    public AdventureBook book;

    [Tooltip("Optional. If set, one random event is drawn at the end.")]
    public EventPool pool;

    void Start()
    {
        AdventureRunner runner;

        if (book != null)
        {
            runner = book.StartNewAdventure();
        }
        else
        {
            Debug.Log("No book assigned - running the built-in demo adventure.");
            runner = new AdventureRunner();
            runner.state.SetStat("gold", 12);
            runner.Go(BuildDemoAdventure());
        }

        PlayThrough(runner);

        if (pool != null)
        {
            Debug.Log("--- drawing a random event ---");

            AdventureEvent random = pool.Draw(runner.state);

            if (random == null)
            {
                Debug.Log("Nothing in the pool is possible right now.");
            }
            else
            {
                runner.Go(random);
                PlayThrough(runner);
            }
        }
    }

    // Plays automatically, always taking the first option the player is allowed
    // to pick. Your real game would wait for a button press instead.
    void PlayThrough(AdventureRunner runner)
    {
        int safetyLimit = 25; // stops an accidental loop from hanging Unity

        while (!runner.IsFinished() && safetyLimit > 0)
        {
            safetyLimit--;

            AdventureEvent evt = runner.GetCurrentEvent();

            Debug.Log("=== " + evt.GetTitle() + " ===\n"
                      + evt.bodyText
                      + "\n(gold: " + runner.state.GetStat("gold") + ")");

            List<Choice> options = runner.GetVisibleChoices();

            if (options.Count == 0)
            {
                Debug.Log("[the end]");
                return;
            }

            Choice picked = null;

            foreach (Choice option in options)
            {
                bool allowed = option.IsAvailable(runner.state);

                string line = (allowed ? "   [ ] " : "   [x] ") + option.text;

                string cost = option.DescribeCost();
                if (cost != "") line += "   (" + cost + ")";

                if (!allowed) line += "   <- " + option.DescribeRequirements();

                Debug.Log(line);

                // Remember the first one we are actually allowed to take.
                if (allowed && picked == null) picked = option;
            }

            if (picked == null)
            {
                Debug.LogWarning("Every option is locked - the player is stuck. "
                                 + "Always leave one option with no requirements!");
                return;
            }

            Debug.Log(">> picking: " + picked.text);
            runner.Choose(picked);

            // The escape hatch. This is where you react to the things flags and
            // numbers cannot express.
            if (runner.lastCustomTag != "")
            {
                Debug.Log("   custom tag fired: " + runner.lastCustomTag);

                switch (runner.lastCustomTag)
                {
                    case "TROLL_BOWS":
                        Debug.Log("   (your code would play the bowing animation here)");
                        break;
                }
            }
        }
    }

    // ---- keeping your own Player class in sync -------------------------------
    //
    // The kit owns its own numbers. If your game already has a player.gold, you
    // do NOT need an adapter or an interface. Just copy in and out around the
    // event:
    //
    //     runner.state.SetStat("gold", player.gold);   // before showing it
    //     runner.state.SetStat("hp",   player.hp);
    //
    //     ...player picks an option...
    //
    //     player.gold = runner.state.GetStat("gold");  // after
    //     player.hp   = runner.state.GetStat("hp");
    //
    // Two lines each way, no architecture. Do it in whatever script opens the
    // event window.
    // -------------------------------------------------------------------------

    // Builds three events in code, purely so the demo runs with no setup.
    // You would normally create these as assets in the Project window.
    AdventureEvent BuildDemoAdventure()
    {
        AdventureEvent toll = MakeEvent("The Toll Bridge",
            "A troll squats on the bridge, picking its teeth. \"Ten gold,\" it grunts.");

        AdventureEvent across = MakeEvent("Across",
            "You reach the far bank, lighter of purse but dry of boot.");

        AdventureEvent marsh = MakeEvent("The Long Way",
            "Three hours through the marsh. Your boots will never recover.");

        // Option 1: costs gold, and is only offered if you can afford it.
        Choice pay = new Choice();
        pay.text = "Pay the toll";
        pay.requirements.Add(MakeRequirement(Requirement.Kind.StatAtLeast, "gold", 10));
        pay.effects.Add(MakeEffect(Effect.Kind.AddToStat, "gold", -10));
        pay.goesTo = across;
        toll.choices.Add(pay);

        // Option 2: no requirements, so always available. Every event should
        // have at least one of these or the player can get stuck.
        Choice walk = new Choice();
        walk.text = "Take the long way round";
        walk.effects.Add(MakeEffect(Effect.Kind.SetFlag, "muddy_boots", 0));
        walk.goesTo = marsh;
        toll.choices.Add(walk);

        // Option 3: needs a flag the player does not have, so it shows up
        // greyed out with its reason. This is the teasing-the-player case.
        Choice seal = new Choice();
        seal.text = "Show the troll your royal seal";
        seal.requirements.Add(MakeRequirement(Requirement.Kind.HasFlag, "royal_seal", 0));
        seal.customTag = "TROLL_BOWS";
        seal.goesTo = across;
        toll.choices.Add(seal);

        // "across" and "marsh" have no choices, so they are endings.
        return toll;
    }

    // ---- little builders, only needed because this demo has no assets ----

    AdventureEvent MakeEvent(string title, string body)
    {
        AdventureEvent evt = ScriptableObject.CreateInstance<AdventureEvent>();
        evt.name = title;
        evt.title = title;
        evt.bodyText = body;
        return evt;
    }

    Requirement MakeRequirement(Requirement.Kind kind, string key, int value)
    {
        Requirement requirement = new Requirement();
        requirement.kind = kind;
        requirement.key = key;
        requirement.value = value;
        return requirement;
    }

    Effect MakeEffect(Effect.Kind kind, string key, int value)
    {
        Effect effect = new Effect();
        effect.kind = kind;
        effect.key = key;
        effect.value = value;
        return effect;
    }
}
