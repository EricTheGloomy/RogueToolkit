using System.Collections.Generic;
using UnityEngine;

// A throwaway demo. Put it on any GameObject and press Play.
//
// Leave the "library" slot EMPTY and it builds four actions in code, so you can
// watch the whole thing work with zero setup. It runs the SAME timeline twice:
//
//   1. Turn-based, instantly, in Start()  - so you see the order straight away.
//   2. Real time, over the next 6 seconds - so you see it actually play out.
//
// Delete this file once it clicks. Nothing else needs it.

public class TimelineExample : MonoBehaviour
{
    [Tooltip("Optional. Leave empty to run the built-in demo actions.")]
    public TimelineLibrary library;

    private TimelineRunner runner = new TimelineRunner();

    void Start()
    {
        Debug.Log("--- 1. TURN BASED: StepOne() until it is done ---");
        RunTurnBased();

        Debug.Log("--- 2. REAL TIME: Tick(Time.deltaTime) in Update ---");
        runner.Play(BuildTimeline());
    }

    void Update()
    {
        // This is the whole real-time integration. Two lines.
        foreach (TimelineSignal signal in runner.Tick(Time.deltaTime))
        {
            Handle(signal);
        }
    }

    // ---- the bit you would actually write in your game -----------------------

    void Handle(TimelineSignal signal)
    {
        if (signal.kind == TimelineSignal.Kind.ActionStarted)
        {
            // THIS is where your game does the thing. Deal the damage, start the
            // build, play the card, spawn the wave.
            Debug.Log("  FIRE  " + signal.action.GetDisplayName());

            // The escape hatch, same idea as the adventure kit.
            if (signal.action.customTag == "BIG_ONE")
            {
                Debug.Log("        (your code would shake the camera here)");
            }

            // If the action said waitForFinish, the timeline is now parked and
            // will not move until somebody calls FinishCurrent(). In a real game
            // you would call it from an animation event or a coroutine. Here we
            // just let it go immediately so the demo keeps moving.
            if (runner.IsWaiting())
            {
                Debug.Log("        (parked - waiting for FinishCurrent)");

                foreach (TimelineSignal more in runner.FinishCurrent())
                {
                    Handle(more);
                }
            }
        }
        else if (signal.kind == TimelineSignal.Kind.ActionFinished)
        {
            Debug.Log("  done  " + signal.action.GetDisplayName());
        }
        else if (signal.kind == TimelineSignal.Kind.TimelineFinished)
        {
            Debug.Log("  === timeline finished ===");
        }
    }

    // ---- turn-based version of exactly the same timeline ---------------------

    void RunTurnBased()
    {
        TimelineRunner turnRunner = new TimelineRunner();
        turnRunner.Play(BuildTimeline());

        int safetyLimit = 30; // stops a mistake from hanging Unity

        while (turnRunner.IsPlaying() && safetyLimit > 0)
        {
            safetyLimit--;

            foreach (TimelineSignal signal in turnRunner.StepOne())
            {
                // StepOne handles waitForFinish for us, so no special case here.
                if (signal.kind == TimelineSignal.Kind.ActionStarted)
                {
                    Debug.Log("  FIRE  " + signal.action.GetDisplayName());
                }
                else if (signal.kind == TimelineSignal.Kind.TimelineFinished)
                {
                    Debug.Log("  === timeline finished ===");
                }
            }
        }
    }

    // ---- building the timeline ----------------------------------------------

    Timeline BuildTimeline()
    {
        // Using your own library asset if you gave us one.
        if (library != null)
        {
            Timeline fromPreset = library.BuildPreset();

            if (fromPreset.Count > 0) return fromPreset;

            Debug.LogWarning("The library's Preset Sequence is empty, so there is "
                             + "nothing to play. Falling back to the demo actions.");
        }

        // Otherwise build four actions and a timeline in code.
        TimelineAction quickJab  = MakeAction("Quick Jab", 0.5f, false, "");
        TimelineAction windUp    = MakeAction("Wind Up", 1.5f, false, "");
        TimelineAction haymaker  = MakeAction("Haymaker", 1.0f, false, "BIG_ONE");
        TimelineAction taunt     = MakeAction("Taunt", 0f, true, ""); // instant, but waits

        Timeline timeline = new Timeline();

        // Room for five things, three seconds of them. Try adding a sixth and
        // watch Add() return false.
        timeline.maxEntries = 5;
        timeline.maxTotalDuration = 0f; // no limit on length, just on count

        timeline.Add(quickJab);
        timeline.Add(quickJab);
        timeline.Add(windUp);
        timeline.Add(haymaker, 0.5f); // half a second of dead air first
        timeline.Add(taunt);

        Debug.Log("Built a timeline of " + timeline.Count + " actions, "
                  + timeline.GetTotalDuration() + "s long.");

        return timeline;
    }

    TimelineAction MakeAction(string actionName, float duration, bool waitForFinish, string tag)
    {
        TimelineAction action = ScriptableObject.CreateInstance<TimelineAction>();
        action.name = actionName;
        action.displayName = actionName;
        action.duration = duration;
        action.waitForFinish = waitForFinish;
        action.customTag = tag;
        return action;
    }
}
