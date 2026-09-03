# Simple Adventure for Unity

Version 1 — 31 August 2026. Choose-your-own-adventure pages and random events,
one system. Completely separate from the Simple Node Graph kit — you can use
either, both, or neither.

## What's in here

| File | What it is |
|---|---|
| `Requirement.cs` | One condition: "needs 10 gold", "must have the key". |
| `Effect.cs` | One consequence: "gain 10 gold", "remember this". |
| `Choice.cs` | One option the player can pick. Edited inside its event. |
| `AdventureEvent.cs` | One page: text plus options. Also what a random event is. |
| `AdventureState.cs` | What the player has: flags, numbers, events already seen. |
| `AdventureRunner.cs` | Runs it: current page, valid options, picking one. |
| `AdventureBook.cs` | The asset your game points at. Knows every event, loads saves. |
| `EventPool.cs` | A bag of random events with weights and conditions. |
| `AdventureExample.cs` | Zero-setup demo. Press Play, read the Console. Delete later. |
| `HowToUse.html` | The full guide. Open in a browser — not a Unity file. |

Nine files sounds like a lot; each is small and does exactly one thing. The
first four are the pieces you author with, the next three run it.

## Install

1. Copy the nine `.cs` files into your project, anywhere under `Assets`.
2. **Move `HowToUse.html` and `README.md` out of `Assets`.**
3. Add the **Adventure Example** component to an empty GameObject and press
   Play. Leave its slots empty — it builds a demo adventure in code so you see
   the whole thing working before you author anything.

You should see a toll bridge, three options (one priced, one free, one greyed
out with its reason), and the result of picking.

## The idea

An **event** is a page: some text, and a list of **choices**. A choice can have
**requirements** (can I pick it?) and **effects** (what happens). A choice
points at the next event — or at nothing, which ends the adventure.

A **random event** is the same thing. The only difference is how you got there:
a story page comes from another page's choice, a random event gets pulled out of
an **EventPool**. One asset type, one thing to learn.

The player's memory is deliberately just three things:

| | | |
|---|---|---|
| **Flags** | on/off facts | `has_rusty_key`, `spared_the_wolf` |
| **Stats** | named numbers | `gold` = 40, `hp` = 12 |
| **Seen** | events already had | so "only once" random events work |

You author every requirement and effect in the Inspector with dropdowns. No code
per event.

## The 60-second version

```csharp
// Start
AdventureRunner runner = book.StartNewAdventure();

// Draw a random event instead
AdventureEvent evt = pool.Draw(runner.state);
if (evt != null) runner.Go(evt);

// Draw the UI
AdventureEvent page = runner.GetCurrentEvent();
foreach (Choice option in runner.GetVisibleChoices())
{
    bool allowed = option.IsAvailable(runner.state);
    // allowed  -> a real button, option.text, option.DescribeCost()
    // !allowed -> greyed out, option.DescribeRequirements() explains why
}

// Player clicks
runner.Choose(option);          // returns false and changes nothing if not allowed
if (runner.IsFinished()) { /* close the window */ }

// Save and load
string json = JsonUtility.ToJson(runner.Save());
AdventureRunner resumed = book.Resume(JsonUtility.FromJson<AdventureSave>(json));
```

## Your own Player class

The kit owns its own numbers. If your game already has `player.gold`, you do
**not** need an adapter. Copy in and out around the event:

```csharp
runner.state.SetStat("gold", player.gold);   // before showing it
// ...player picks an option...
player.gold = runner.state.GetStat("gold");  // after
```

Two lines each way. No architecture.

## The escape hatch

For things flags and numbers can't say, put a tag on the choice and switch on it:

```csharp
runner.Choose(option);

switch (runner.lastCustomTag)
{
    case "SPAWN_BOSS":    spawner.SpawnBoss(); break;
    case "PLAY_CUTSCENE": cutscene.Play();     break;
}
```

## Three things that will bite you

**Always leave one option with no requirements.** If every option on a page is
locked, the player is stuck with nothing to click and no way out. The demo
script logs a warning when it spots this.

**Ctrl+D copies an event's hidden ID.** Duplicating an event asset copies its ID
too, so two events look identical to your save file. **Collect Events** on your
Book detects this and names both assets; the fix is right-click the copy →
**Assign New Id**. Re-run Collect Events after duplicating.

**Pool-only events need to be in the Book.** `Collect Events` walks the story
from `firstEvent` through the choices, so it can't find events that only exist
inside an `EventPool`. Drag those into the Book's **alsoInclude** list, or
saving won't be able to find them again. `pool.GetAllEvents()` gives you the
list to drag.

## Notes

`effectsOnArrival` fires every time the player arrives, including a return
visit. That's usually what you want for a random event and usually *not* what
you want for a story page you can loop back to — use a flag to guard it if so.

Loading a save uses `ResumeAt`, not `Go`, precisely so arrival effects are not
handed out a second time on every reload. If you write your own load path, do
the same.

Numbers can go negative. If `hp` shouldn't drop below zero, clamp it in your own
code — the kit doesn't guess at your rules.

## Verified

102 automated tests pass, covering: every requirement and effect kind, boundary
values, unset keys, null and empty Inspector slots, save/load round-trips
including truncated save files, choice validation (refusing locked options and
options from the wrong event), the custom tag surviving a page move, endings,
story walking with loops, duplicate-ID detection, resume not re-applying arrival
effects, and weighted random draws (measured distribution, conditions,
once-only, and running dry). Compiles clean in both editor and player builds.
