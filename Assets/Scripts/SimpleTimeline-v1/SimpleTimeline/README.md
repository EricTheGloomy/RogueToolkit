# Simple Timeline for Unity

Version 1 — 31 August 2026. Put X things in order, press start, they fire one
after another. Works as a real-time queue or a turn-based one, off the same
list. Completely separate from the Node Graph and Adventure kits.

> **Not related to Unity's own Timeline package.** The names don't actually
> collide, because Unity's live in the `UnityEngine.Timeline` namespace and
> these are global. But if you ever get an "ambiguous reference" error in a file
> that has `using UnityEngine.Timeline;`, that's why — delete that using, or
> spell this kit's class as `global::Timeline`.

## What's in here

| File | What it is |
|---|---|
| `TimelineAction.cs` | One thing that can happen, and how long it takes. |
| `Timeline.cs` | The ordered list. Add, remove, reorder, capacity limits, timing. |
| `TimelineSignal.cs` | "Something just happened." What the runner hands back. |
| `TimelineRunner.cs` | Walks the playhead along and tells you what happened. |
| `TimelineLibrary.cs` | The asset your game points at: palette, lookup, presets. |
| `TimelineExample.cs` | Zero-setup demo. Press Play, read the Console. Delete later. |
| `HowToUse.html` | The full guide. Open in a browser — not a Unity file. |

## Install

1. Copy the six `.cs` files into your project, anywhere under `Assets`.
2. **Move `HowToUse.html` and `README.md` out of `Assets`.**
3. Add the **Timeline Example** component to an empty GameObject and press Play.
   Leave its slot empty — it builds five actions in code so you see the whole
   thing working before authoring anything.

You'll see the same five-action timeline run twice: instantly turn-based, then
over four real seconds.

## The idea

A **TimelineAction** asset says "this exists, it's called X, it takes Y long".
It does **not** know how to do the thing — your code does that when the runner
says the action started. That split is why one small kit serves cards, attacks
and production queues alike: the timeline only ever deals with **order** and
**length**.

A **Timeline** is the ordered list. Start and end times aren't stored — they're
worked out by stacking the entries up, so reordering or removing something
re-times everything after it with no bookkeeping to get wrong.

The **runner** doesn't call your methods and doesn't use C# events. `Tick()`
hands you back a list of signals and you look through it:

```csharp
foreach (TimelineSignal signal in runner.Tick(Time.deltaTime))
{
    if (signal.kind == TimelineSignal.Kind.ActionStarted)
    {
        DoTheThing(signal.action);   // <- your game happens here
    }
}
```

Nine times out of ten `ActionStarted` is the only signal you care about.

## Two ways to drive it, same list

**Real time** — call `Tick(Time.deltaTime)` every frame from `Update()`.
Durations mean seconds. Production queues, attack sequences, spawn waves.

**Turn based** — call `StepOne()` when you want the next thing to happen.
Durations stop mattering entirely. Card games, puzzle games, cutscenes.

You can build the timeline the same way for both and decide per project.

## The 60-second version

```csharp
// Build a list
Timeline timeline = library.BuildEmpty(maxEntries: 5, maxTotalDuration: 0f);
timeline.Add(someAction);
timeline.Add(otherAction, delayBefore: 0.5f);
timeline.Move(0, 2);                  // drag-to-reorder
if (!timeline.CanAdd(action)) { /* palette button greyed out */ }

// Play it
runner.Play(timeline);
runner.Pause();  runner.Resume();  runner.Stop();
runner.speed = 2f;
runner.loop = true;

// Drive it (pick one)
foreach (TimelineSignal s in runner.Tick(Time.deltaTime)) { Handle(s); }
foreach (TimelineSignal s in runner.StepOne())            { Handle(s); }

// Progress bar
bar.fillAmount = runner.GetProgress();

// Save and load
string json = JsonUtility.ToJson(timeline.Save());
timeline.Load(JsonUtility.FromJson<TimelineSave>(json), library);
```

## Making it yours

Inherit from `TimelineAction` and add whatever your actions need:

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Timeline/Attack")]
public class AttackAction : TimelineAction
{
    public int damage;
    public GameObject effectPrefab;
}
```

Then in your handler, cast it:

```csharp
AttackAction attack = (AttackAction)signal.action;
enemy.TakeDamage(attack.damage);
```

## When the real length isn't a number

Tick **Wait For Finish** on an action and the timeline stops dead the moment it
starts, until your code says otherwise:

```csharp
// from an animation event, a coroutine, a server callback...
foreach (TimelineSignal s in runner.FinishCurrent()) { Handle(s); }
```

Time genuinely doesn't move while parked — `runner.IsWaiting()` is true and
`Tick()` does nothing however long you tick it. `StepOne()` treats a parked
action as done, so turn-based games need no special case.

## Three things worth knowing

**Only one action runs at a time.** That's the point of "one after another". If
you need two things overlapping, use two runners with two timelines.

**Pause and Stop are different.** `Pause` freezes; `Resume` picks up exactly
where it left off. `Stop` cancels and rewinds, and after it you must call `Play`
again — `Resume` and `StepOne` deliberately won't revive a stopped timeline, so
a cancelled attack can't come back to life because something called `Resume` out
of habit.

**Ctrl+D copies an action's hidden ID.** Duplicating an action asset copies its
ID too, so two actions look identical to your save file. Right-click your
Library asset → **Check For Problems** catches it, along with empty slots and
preset actions missing from the palette. Run it after duplicating.

## Notes

A big `deltaTime` — a lag spike, or an alt-tab — never skips an action. One
`Tick` reports everything the playhead passed, in order, so a 100-second tick on
a 3-second timeline gives you all six start/finish signals plus the end.

At a boundary with no gap, `ActionFinished` always arrives before the next
`ActionStarted`. That's guaranteed, not incidental — it's what lets you clean up
one thing before the next begins.

Zero-duration actions start and finish in the same moment, which is what you
want for instant effects. A timeline made *entirely* of them has no length, so
`loop` would spin forever — the runner detects that, warns, and switches loop
off rather than freezing Unity.

Negative durations and gaps read as zero, so a stray minus sign in the Inspector
can't run the playhead backwards.

## Verified

115 automated tests pass, covering: list editing and reordering, both capacity
limits, timing maths and re-timing after edits, signal ordering at exact
boundaries, one enormous tick versus 400 tiny ones, gaps, instant actions,
speed, progress, pause/resume/stop semantics, looping and the zero-length loop
guard, wait-for-finish parking, turn-based stepping including over a parked
action, null and empty inputs throughout, library problem detection, and
save/load round-trips including renamed assets, deleted actions and truncated
save files. Compiles clean in both editor and player builds.
