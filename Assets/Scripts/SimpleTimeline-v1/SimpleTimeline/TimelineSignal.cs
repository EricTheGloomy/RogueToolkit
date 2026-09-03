// "SOMETHING JUST HAPPENED ON THE TIMELINE."
//
// The runner does not call your methods and does not use C# events. Instead,
// Tick() hands you back a list of these and you look through it. That keeps the
// whole thing easy to follow, easy to debug, and easy to test:
//
//     foreach (TimelineSignal signal in runner.Tick(Time.deltaTime))
//     {
//         if (signal.kind == TimelineSignal.Kind.ActionStarted)
//         {
//             DoTheThing(signal.action);
//         }
//     }
//
// Nine times out of ten ActionStarted is the only one you care about.

public class TimelineSignal
{
    public enum Kind
    {
        ActionStarted,     // fire your effect HERE - this is the important one
        ActionFinished,    // its duration ran out (or you called FinishCurrent)
        TimelineFinished,  // the whole list is done
    }

    public Kind kind;

    // Which slot on the timeline. -1 for TimelineFinished.
    public int index = -1;

    // What was placed there. null for TimelineFinished, and also null if
    // somebody left an empty slot on the timeline.
    public TimelineAction action;

    public TimelineSignal(Kind kind, int index, TimelineAction action)
    {
        this.kind = kind;
        this.index = index;
        this.action = action;
    }

    // So Debug.Log(signal) prints something readable.
    public override string ToString()
    {
        if (kind == Kind.TimelineFinished) return "TimelineFinished";

        string actionName = (action != null) ? action.GetDisplayName() : "<empty slot>";

        return kind + " [" + index + "] " + actionName;
    }
}
