using System.Collections.Generic;
using UnityEngine;

// PLAYS A TIMELINE. Walks a playhead along the list and tells you what happened.
//
// There are two ways to drive it, and they use the same timeline and the same
// actions - pick whichever suits the project:
//
//   REAL TIME    call Tick(Time.deltaTime) every frame from Update().
//                Durations mean seconds. Production queues, attack sequences.
//
//   TURN BASED   call StepOne() when you want the next thing to happen.
//                Durations stop mattering. Card games, puzzle games.
//
// Only ONE action runs at a time - the point of a timeline is "one after
// another". If you need two things overlapping, use two runners.

public class TimelineRunner
{
    // Play the list again from the start when it ends.
    public bool loop = false;

    // 2 means double speed, 0.5 means half. Only affects Tick().
    public float speed = 1f;

    private Timeline timeline;

    private float position;      // where the playhead is, in the same units as duration
    private int nextToStart;     // the slot we have not started yet
    private int running = -1;    // the slot currently running, -1 for none
    private bool playing;
    private bool waiting;        // parked on a waitForFinish action
    private bool stopped;        // Stop() was called, so Resume() must not revive it

    // ---------------- reading ----------------

    public bool IsPlaying() { return playing; }

    // True while parked on a waitForFinish action, waiting for your code to
    // call FinishCurrent(). Time does not move while this is true.
    public bool IsWaiting() { return waiting; }

    public float GetPosition() { return position; }

    public Timeline GetTimeline() { return timeline; }

    public int GetRunningIndex() { return running; }

    public TimelineAction GetRunningAction()
    {
        if (timeline == null || running < 0) return null;

        return timeline.GetAction(running);
    }

    // 0 to 1, for a progress bar.
    public float GetProgress()
    {
        if (timeline == null) return 0f;

        float total = timeline.GetTotalDuration();

        // A timeline of nothing but instant actions has no length to measure,
        // so it is either not started (0) or done (1).
        if (total <= 0f)
        {
            return (nextToStart >= timeline.Count) ? 1f : 0f;
        }

        float progress = position / total;

        if (progress < 0f) progress = 0f;
        if (progress > 1f) progress = 1f;

        return progress;
    }

    // ---------------- control ----------------

    // Loads a timeline and starts from the beginning. Check IsPlaying() after -
    // an empty timeline has nothing to play.
    public void Play(Timeline timelineToPlay)
    {
        timeline = timelineToPlay;

        position = 0f;
        nextToStart = 0;
        running = -1;
        waiting = false;
        stopped = false;

        playing = (timeline != null && timeline.Count > 0);
    }

    // Freezes the playhead. Whatever was running stays running, and Resume()
    // picks up exactly where it left off.
    public void Pause()
    {
        playing = false;
    }

    public void Resume()
    {
        if (timeline == null) return;

        // Stop() is a cancel, not a pause - you have to Play() again after it.
        if (stopped) return;

        // Do not revive a timeline that simply ran out, either.
        bool anythingLeft = (nextToStart < timeline.Count) || (running >= 0) || waiting;

        if (anythingLeft) playing = true;
    }

    // Cancels and rewinds to the start. Does NOT emit TimelineFinished, because
    // the timeline did not finish - you stopped it.
    //
    // This is different from Pause: after Stop you have to call Play() again.
    // Resume() and StepOne() will not restart it, so a cancelled attack cannot
    // come back to life because something called Resume out of habit.
    public void Stop()
    {
        playing = false;
        waiting = false;
        stopped = true;
        position = 0f;
        nextToStart = 0;
        running = -1;
    }

    // ---------------- driving it ----------------

    // Call every frame from Update: runner.Tick(Time.deltaTime).
    // Returns a fresh list of everything that happened. Usually empty.
    public List<TimelineSignal> Tick(float deltaTime)
    {
        List<TimelineSignal> signals = new List<TimelineSignal>();

        if (!playing || timeline == null) return signals;

        // Parked on a waitForFinish action: time stands still.
        if (waiting) return signals;

        if (deltaTime > 0f)
        {
            position += deltaTime * speed;
        }

        Process(signals);

        return signals;
    }

    // Turn-based use: forget about seconds and just make the next thing happen.
    // Call Play() once, then StepOne() whenever you want the next action.
    //
    // Does nothing if the runner is paused, stopped or finished - stepping is
    // "advance the thing that is playing", never "start playing".
    public List<TimelineSignal> StepOne()
    {
        List<TimelineSignal> signals = new List<TimelineSignal>();

        if (timeline == null || !playing) return signals;

        // If we are parked on a waitForFinish action, "step" means "that's done".
        if (waiting)
        {
            waiting = false;

            if (running >= 0) position = timeline.GetEndTime(running);
        }

        // Jump the playhead to wherever the next action begins - or to the very
        // end if there is no next action, so the timeline can finish.
        if (nextToStart < timeline.Count)
        {
            float target = timeline.GetStartTime(nextToStart);

            if (target > position) position = target;
        }
        else
        {
            position = timeline.GetTotalDuration();
        }

        Process(signals);

        return signals;
    }

    // Call this when a waitForFinish action's real work is over: the animation
    // ended, the player confirmed, the server replied.
    public List<TimelineSignal> FinishCurrent()
    {
        List<TimelineSignal> signals = new List<TimelineSignal>();

        // Nothing is waiting, so there is nothing to finish.
        if (timeline == null || !waiting) return signals;

        waiting = false;

        if (running >= 0)
        {
            // Snap the playhead to the end of this action so everything after it
            // is still timed from the right place.
            position = timeline.GetEndTime(running);
        }

        Process(signals);

        return signals;
    }

    // ---------------- the actual engine ----------------
    //
    // Works out everything the playhead has passed since last time. It is a
    // loop rather than a single check because one Tick with a big deltaTime -
    // or a lag spike - can pass several short actions at once, and none of them
    // should get skipped.

    private void Process(List<TimelineSignal> signals)
    {
        int guard = 0;

        while (playing && !waiting)
        {
            guard++;
            if (guard > 10000)
            {
                Debug.LogWarning("TimelineRunner: gave up after 10000 steps in one go. "
                                 + "Something is wrong with the timeline's durations.");
                playing = false;
                break;
            }

            // 1. Has the running action's time run out?
            //
            //    Checked FIRST so that when one action ends at exactly the moment
            //    the next begins - which is the normal case, with no gaps - you
            //    always get ActionFinished before the next ActionStarted.
            if (running >= 0 && position >= timeline.GetEndTime(running))
            {
                signals.Add(new TimelineSignal(
                    TimelineSignal.Kind.ActionFinished, running, timeline.GetAction(running)));

                running = -1;
                continue;
            }

            // 2. Has the next action's turn arrived?
            //
            //    Requires nothing else to be running: one at a time.
            if (running < 0
                && nextToStart < timeline.Count
                && position >= timeline.GetStartTime(nextToStart))
            {
                int index = nextToStart;
                nextToStart++;
                running = index;

                TimelineAction action = timeline.GetAction(index);

                signals.Add(new TimelineSignal(
                    TimelineSignal.Kind.ActionStarted, index, action));

                // Park here until somebody calls FinishCurrent().
                if (action != null && action.waitForFinish)
                {
                    waiting = true;
                }

                continue;
            }

            // 3. Is the whole thing done?
            if (running < 0 && nextToStart >= timeline.Count)
            {
                signals.Add(new TimelineSignal(
                    TimelineSignal.Kind.TimelineFinished, -1, null));

                if (loop)
                {
                    // A timeline with no length would loop forever inside this
                    // one call and freeze Unity, so refuse instead of hanging.
                    if (timeline.GetTotalDuration() <= 0f)
                    {
                        Debug.LogWarning("TimelineRunner: loop is on but the timeline has "
                                         + "no length, which would spin forever. "
                                         + "Turning loop off.");
                        loop = false;
                        playing = false;
                        break;
                    }

                    position = 0f;
                    nextToStart = 0;
                    continue;
                }

                playing = false;
                break;
            }

            // Nothing more has happened yet - the playhead is mid-action or
            // sitting in a gap. Come back next frame.
            break;
        }
    }
}
