using System.Collections.Generic;
using UnityEngine;

// A THROWAWAY VIEW for the SimpleTimeline kit.
//
// HOW TO USE: add this to any empty GameObject and press Play. That is all.
//
//   click a palette button      add that action to the end of the timeline
//   click a segment             remove it
//   Play / Pause / Stop         the obvious
//   Step                        turn-based mode: advance one action
//   0.5x / 1x / 2x              speed, real-time only
//   Loop                        start over when it ends
//
// This is the picture from the guide, live: a strip of stacked actions with a
// playhead walking along it, and the signals printed as they arrive.
//
// Delete this file when you are done with it.

public class TimelinePrototypeView : MonoBehaviour
{
    [Tooltip("Leave empty to use the built-in demo actions.")]
    public TimelineLibrary library;

    [Header("Look")]
    public int pixelsPerSecond = 110;
    public int barLeft = 40;
    public int barTop = 110;
    public int barHeight = 56;

    private Timeline timeline;
    private TimelineRunner runner = new TimelineRunner();
    private List<TimelineAction> palette = new List<TimelineAction>();
    private List<string> recent = new List<string>();

    private Texture2D dot;

    private static readonly Color background = new Color(0.11f, 0.12f, 0.15f);
    private static readonly Color track = new Color(0.17f, 0.18f, 0.22f);
    private static readonly Color idle = new Color(0.27f, 0.29f, 0.42f);
    private static readonly Color firing = new Color(0.62f, 0.64f, 0.94f);
    private static readonly Color waitingColour = new Color(0.85f, 0.64f, 0.34f);
    private static readonly Color playhead = new Color(0.95f, 0.95f, 0.98f);
    private static readonly Color gapColour = new Color(0.14f, 0.15f, 0.18f);

    void Start()
    {
        if (library != null)
        {
            foreach (TimelineAction action in library.allActions)
            {
                if (action != null) palette.Add(action);
            }
        }

        if (palette.Count == 0) BuildDemoActions();

        timeline = new Timeline();
        timeline.maxEntries = 8;

        // Start with something on it so there is a picture to look at.
        timeline.Add(palette[0]);
        timeline.Add(palette[0]);
        timeline.Add(palette[1]);
        timeline.Add(palette[2], 0.4f);

        recent.Clear();
    }

    void Update()
    {
        // The whole real-time integration, exactly as in the guide.
        foreach (TimelineSignal signal in runner.Tick(Time.deltaTime))
        {
            Note(signal.ToString());

            // A waitForFinish action parks the timeline. In a real game you
            // would release it from an animation event; here a button does it.
        }
    }

    void OnGUI()
    {
        EnsureDot();
        Box(new Rect(0, 0, Screen.width, Screen.height), background);

        GUI.Label(new Rect(barLeft, 16, 800, 22),
                  "SimpleTimeline prototype view - build a timeline, then play it");

        DrawTransport();
        DrawBar();
        DrawPalette();
        DrawLog();
    }

    // ---------------- drawing ----------------

    void DrawTransport()
    {
        int x = barLeft;
        int y = 46;

        if (GUI.Button(new Rect(x, y, 70, 26), "Play")) { runner.Play(timeline); recent.Clear(); }
        x += 76;

        if (GUI.Button(new Rect(x, y, 70, 26), runner.IsPlaying() ? "Pause" : "Resume"))
        {
            if (runner.IsPlaying()) runner.Pause(); else runner.Resume();
        }
        x += 76;

        if (GUI.Button(new Rect(x, y, 70, 26), "Stop")) runner.Stop();
        x += 76;

        if (GUI.Button(new Rect(x, y, 70, 26), "Step"))
        {
            foreach (TimelineSignal signal in runner.StepOne()) Note(signal.ToString());
        }
        x += 90;

        // Speed only affects Tick, not Step.
        GUI.Label(new Rect(x, y + 4, 46, 22), "speed");
        x += 46;
        if (GUI.Button(new Rect(x, y, 42, 26), "0.5x")) runner.speed = 0.5f;
        x += 46;
        if (GUI.Button(new Rect(x, y, 42, 26), "1x")) runner.speed = 1f;
        x += 46;
        if (GUI.Button(new Rect(x, y, 42, 26), "2x")) runner.speed = 2f;
        x += 60;

        if (GUI.Button(new Rect(x, y, 80, 26), runner.loop ? "Loop ON" : "Loop off"))
        {
            runner.loop = !runner.loop;
        }
        x += 90;

        if (runner.IsWaiting())
        {
            if (GUI.Button(new Rect(x, y, 130, 26), "Finish current"))
            {
                foreach (TimelineSignal signal in runner.FinishCurrent()) Note(signal.ToString());
            }
        }
    }

    void DrawBar()
    {
        float totalWidth = timeline.GetTotalDuration() * pixelsPerSecond;

        // The empty track, so a short timeline still reads as a strip.
        Box(new Rect(barLeft, barTop, Mathf.Max(totalWidth, 400), barHeight), track);

        Event e = Event.current;

        for (int i = 0; i < timeline.Count; i++)
        {
            Timeline.Entry entry = timeline.GetEntry(i);
            if (entry == null) continue;

            // The gap before this entry, drawn hollow.
            if (entry.GetDelay() > 0f)
            {
                float gapStart = barLeft + (timeline.GetStartTime(i) - entry.GetDelay()) * pixelsPerSecond;
                Box(new Rect(gapStart, barTop + 8, entry.GetDelay() * pixelsPerSecond - 2, barHeight - 16), gapColour);
            }

            float start = barLeft + timeline.GetStartTime(i) * pixelsPerSecond;
            float width = Mathf.Max(entry.GetActionDuration() * pixelsPerSecond - 2, 14);

            Rect area = new Rect(start, barTop, width, barHeight);

            bool isRunning = (runner.GetRunningIndex() == i);
            bool parked = isRunning && runner.IsWaiting();

            Box(area, parked ? waitingColour : (isRunning ? firing : idle));

            GUI.Label(new Rect(area.x + 5, area.y + 6, width + 60, 20), entry.action.GetDisplayName());
            GUI.Label(new Rect(area.x + 5, area.y + 26, width + 60, 20),
                      entry.action.GetDuration() + "s" + (entry.action.waitForFinish ? "  (waits)" : ""));

            // Click a segment to take it off.
            if (e != null && e.type == EventType.MouseDown && e.button == 0 && Contains(area, e.mousePosition))
            {
                timeline.RemoveAt(i);
                e.Use();
                return;
            }
        }

        // The playhead.
        float head = barLeft + runner.GetPosition() * pixelsPerSecond;
        Box(new Rect(head - 1, barTop - 10, 2, barHeight + 20), playhead);

        // A ruler, one tick per second.
        int seconds = Mathf.FloorToInt(timeline.GetTotalDuration()) + 1;
        for (int s = 0; s <= seconds; s++)
        {
            float x = barLeft + s * pixelsPerSecond;
            Box(new Rect(x, barTop + barHeight + 4, 1, 5), track);
            GUI.Label(new Rect(x - 6, barTop + barHeight + 10, 40, 20), s + "s");
        }

        GUI.Label(new Rect(barLeft, barTop + barHeight + 34, 500, 22),
                  "length " + timeline.GetTotalDuration().ToString("0.0") + "s"
                  + "     playhead " + runner.GetPosition().ToString("0.00") + "s"
                  + "     progress " + Mathf.RoundToInt(runner.GetProgress() * 100) + "%"
                  + (runner.IsPlaying() ? "" : "   [stopped]"));
    }

    void DrawPalette()
    {
        int top = barTop + barHeight + 76;

        GUI.Label(new Rect(barLeft, top, 400, 22),
                  "ADD  (" + timeline.Count + " / " + timeline.maxEntries + " slots used)");

        for (int i = 0; i < palette.Count; i++)
        {
            TimelineAction action = palette[i];
            if (action == null) continue;

            bool room = timeline.CanAdd(action);

            string label = action.GetDisplayName() + "  " + action.GetDuration() + "s";
            if (!room) label += "  (full)";

            if (GUI.Button(new Rect(barLeft + i * 150, top + 24, 140, 26), label) && room)
            {
                timeline.Add(action);
            }
        }

        if (GUI.Button(new Rect(barLeft, top + 60, 140, 26), "Clear timeline"))
        {
            runner.Stop();
            timeline.Clear();
        }

        GUI.Label(new Rect(barLeft + 156, top + 64, 500, 22), "click a segment on the bar to remove it");
    }

    void DrawLog()
    {
        int left = Screen.width - 300;

        GUI.Label(new Rect(left, 46, 280, 22), "SIGNALS");

        for (int i = 0; i < recent.Count; i++)
        {
            GUI.Label(new Rect(left, 68 + i * 18, 280, 18), recent[i]);
        }
    }

    // ---------------- helpers ----------------

    void Note(string line)
    {
        recent.Add(line);

        // Keep the last handful only.
        while (recent.Count > 18) recent.RemoveAt(0);
    }

    bool Contains(Rect area, Vector2 point)
    {
        return point.x >= area.x && point.x <= area.x + area.width
            && point.y >= area.y && point.y <= area.y + area.height;
    }

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

    // ---------------- the demo actions ----------------

    void BuildDemoActions()
    {
        palette.Add(MakeAction("Quick Jab", 0.5f, false));
        palette.Add(MakeAction("Wind Up", 1.5f, false));
        palette.Add(MakeAction("Haymaker", 1.0f, false));
        palette.Add(MakeAction("Taunt", 0.2f, true));   // parks until you release it
    }

    TimelineAction MakeAction(string actionName, float duration, bool waitForFinish)
    {
        TimelineAction action = ScriptableObject.CreateInstance<TimelineAction>();
        action.name = actionName;
        action.displayName = actionName;
        action.duration = duration;
        action.waitForFinish = waitForFinish;
        return action;
    }
}
