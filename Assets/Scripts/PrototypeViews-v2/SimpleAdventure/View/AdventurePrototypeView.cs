using System.Collections.Generic;
using UnityEngine;

// A THROWAWAY VIEW for the SimpleAdventure kit.
//
// HOW TO USE: add this to any empty GameObject and press Play. That is all.
//
// Locked options are shown greyed out WITH THEIR REASON, which is the whole
// point - the player can see what they could have done. That is the piece
// worth copying into your real UI.
//
// Delete this file when you are done with it.

public class AdventurePrototypeView : MonoBehaviour
{
    [Tooltip("Leave empty to use the built-in demo adventure.")]
    public AdventureBook book;

    [Tooltip("Optional. If set, the 'Random event' button draws from it.")]
    public EventPool pool;

    private AdventureRunner runner;
    private string log = "";
    private Texture2D dot;

    private static readonly Color background = new Color(0.12f, 0.11f, 0.14f);
    private static readonly Color panel = new Color(0.17f, 0.16f, 0.20f);
    private static readonly Color available = new Color(0.36f, 0.24f, 0.34f);
    private static readonly Color lockedOption = new Color(0.20f, 0.19f, 0.22f);

    void Start()
    {
        runner = (book != null) ? book.StartNewAdventure() : StartDemo();
        log = "";
    }

    void OnGUI()
    {
        EnsureDot();
        Box(new Rect(0, 0, Screen.width, Screen.height), background);

        int left = 40;
        int width = Mathf.Min(560, Screen.width - 320);

        if (runner.IsFinished())
        {
            GUI.Label(new Rect(left, 60, width, 22), "The adventure is over.");
            DrawSidePanel();
            DrawButtons();
            return;
        }

        AdventureEvent page = runner.GetCurrentEvent();

        // ---- the page ----
        Box(new Rect(left - 12, 40, width + 24, 150), panel);
        GUI.Label(new Rect(left, 52, width, 24), page.GetTitle());
        GUI.Label(new Rect(left, 78, width, 100), page.bodyText);

        // ---- the options ----
        List<Choice> options = runner.GetVisibleChoices();
        int top = 210;

        for (int i = 0; i < options.Count; i++)
        {
            Choice option = options[i];
            bool canPick = option.IsAvailable(runner.state);

            Rect area = new Rect(left - 12, top + i * 52, width + 24, 44);
            Box(area, canPick ? available : lockedOption);

            string label = option.text;

            string cost = option.DescribeCost();
            if (canPick && cost != "") label += "        (" + cost + ")";

            GUI.Label(new Rect(area.x + 12, area.y + 6, area.width - 24, 22), label);

            if (canPick)
            {
                if (GUI.Button(new Rect(area.x + area.width - 90, area.y + 10, 78, 24), "Choose"))
                {
                    Pick(option);
                }
            }
            else
            {
                // THIS is the bit worth copying: say WHY it is locked.
                GUI.Label(new Rect(area.x + 12, area.y + 24, area.width - 24, 20),
                          "locked - " + option.DescribeRequirements());
            }
        }

        if (options.Count == 0)
        {
            GUI.Label(new Rect(left, top, width, 22), "[the end - no options left]");
        }

        DrawSidePanel();
        DrawButtons();
    }

    void DrawSidePanel()
    {
        int right = Screen.width - 250;

        GUI.Label(new Rect(right, 40, 220, 22), "WHAT YOU HAVE");

        int line = 1;

        foreach (string stat in runner.state.GetAllStatNames())
        {
            GUI.Label(new Rect(right, 40 + line * 20, 220, 20),
                      stat + "  " + runner.state.GetStat(stat));
            line++;
        }

        List<string> flags = runner.state.GetAllFlags();

        if (flags.Count > 0) line++;

        foreach (string flag in flags)
        {
            GUI.Label(new Rect(right, 40 + line * 20, 220, 20), flag);
            line++;
        }

        if (line == 1) GUI.Label(new Rect(right, 60, 220, 20), "(nothing yet)");

        if (log != "")
        {
            GUI.Label(new Rect(right, 40 + (line + 2) * 20, 230, 200), log);
        }
    }

    void DrawButtons()
    {
        if (GUI.Button(new Rect(Screen.width - 130, Screen.height - 44, 110, 28), "Restart"))
        {
            Start();
        }

        if (pool == null) return;

        if (GUI.Button(new Rect(Screen.width - 260, Screen.height - 44, 120, 28), "Random event"))
        {
            AdventureEvent drawn = pool.Draw(runner.state);

            if (drawn == null) log = "Nothing in the pool is possible right now.";
            else { runner.Go(drawn); log = "A random event fired."; }
        }
    }

    // ---------------- the bit you would actually write in your game ----------

    void Pick(Choice option)
    {
        if (!runner.Choose(option))
        {
            log = "That option was refused.";
            return;
        }

        // The escape hatch, for the things flags and numbers cannot express.
        if (runner.lastCustomTag != "")
        {
            log = "tag fired: " + runner.lastCustomTag;
        }
        else
        {
            log = "";
        }
    }

    // ---------------- helpers ----------------

    void Box(Rect area, Color colour)
    {
        Color was = GUI.color;
        GUI.color = colour;
        GUI.DrawTexture(area, dot);
        GUI.color = was;
    }

    void EnsureDot()
    {
        if (dot != null) return;
        dot = new Texture2D(1, 1);
        dot.SetPixel(0, 0, Color.white);
        dot.Apply();
    }

    // ---------------- the demo adventure ----------------

    AdventureRunner StartDemo()
    {
        AdventureEvent toll = MakeEvent("The Toll Bridge",
            "A troll squats on the bridge, picking its teeth.\n\"Ten gold,\" it grunts.");

        AdventureEvent across = MakeEvent("Across",
            "You reach the far bank, lighter of purse but dry of boot.");

        AdventureEvent marsh = MakeEvent("The Long Way",
            "Three hours through the marsh. Your boots will never recover.");

        AdventureEvent inn = MakeEvent("The Inn",
            "A fire, a bed, and someone playing a lute badly.");

        // Pay: costs gold, only offered if you can afford it.
        Choice pay = new Choice();
        pay.text = "Pay the toll";
        pay.requirements.Add(MakeRequirement(Requirement.Kind.StatAtLeast, "gold", 10));
        pay.effects.Add(MakeEffect(Effect.Kind.AddToStat, "gold", -10));
        pay.goesTo = across;
        toll.choices.Add(pay);

        // Always available - every page needs at least one of these.
        Choice walk = new Choice();
        walk.text = "Take the long way round";
        walk.effects.Add(MakeEffect(Effect.Kind.SetFlag, "muddy_boots", 0));
        walk.goesTo = marsh;
        toll.choices.Add(walk);

        // Locked, so it shows greyed out with its reason.
        Choice seal = new Choice();
        seal.text = "Show the troll your royal seal";
        seal.requirements.Add(MakeRequirement(Requirement.Kind.HasFlag, "royal_seal", 0));
        seal.customTag = "TROLL_BOWS";
        seal.goesTo = across;
        toll.choices.Add(seal);

        Choice onward = new Choice();
        onward.text = "Walk on to the inn";
        onward.goesTo = inn;
        across.choices.Add(onward);
        marsh.choices.Add(onward);

        Choice sleep = new Choice();
        sleep.text = "Sleep";
        sleep.effects.Add(MakeEffect(Effect.Kind.AddToStat, "rest", 8));
        inn.choices.Add(sleep);

        AdventureRunner demo = new AdventureRunner();
        demo.state.SetStat("gold", 12);
        demo.Go(toll);
        return demo;
    }

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
        Requirement made = new Requirement();
        made.kind = kind;
        made.key = key;
        made.value = value;
        return made;
    }

    Effect MakeEffect(Effect.Kind kind, string key, int value)
    {
        Effect made = new Effect();
        made.kind = kind;
        made.key = key;
        made.value = value;
        return made;
    }
}
