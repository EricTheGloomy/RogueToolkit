# Prototype Views

Version 2 — 2 September 2026. Throwaway visual front-ends for **all five kits**,
so you can *feel* whether a mechanic is fun instead of reading it in the Console.

**v2 adds views for the node graph, adventure and timeline kits.** The grid and
card views are unchanged from v1.

These are add-ons. The kits themselves are byte-for-byte unchanged — that's the
point being demonstrated.

## Install

```
Assets/Kits/SimpleNodeGraph/View/GraphPrototypeView.cs
Assets/Kits/SimpleAdventure/View/AdventurePrototypeView.cs
Assets/Kits/SimpleTimeline/View/TimelinePrototypeView.cs
Assets/Kits/SimpleGrid/View/GridPrototypeView.cs
Assets/Kits/SimpleCards/View/CardPrototypeView.cs
```

Take only the ones whose kit you have. Then add the component to any empty
GameObject and **press Play**. No Canvas, no prefabs, no scene setup, no art,
no packages.

Every view works with no assets at all (it builds a demo in code), or with your
own — each has one optional slot at the top for your Book / Library / Graph.

## Grid view

| | |
|---|---|
| click a palette button, or press 1–4 | pick what to place |
| move the mouse over the board | green/red ghost shows if it fits |
| left click | place it |
| right click a piece | remove it |
| **R** | rotate |
| **C** | clear your layout (game-placed terrain stays) |

Hover a placed piece and the side panel shows its full breakdown — `+3 attack
base`, `+1 attack from 1 touching armour`. That panel is the fastest way to see
why a rule isn't firing.

Try right-clicking the grey Rock. It refuses, because the game placed it.

## Card view

| | |
|---|---|
| click a card's Play button | select it |
| click **Target** on an enemy | play the selected card at it |
| click the same card again | cancel |
| **End Turn** | discard your hand, enemies hit you, draw five |

Watch the message line. Every refusal the kit produces appears there verbatim.
Playing badly on purpose teaches the rules faster than reading them.

## Node graph view

| | |
|---|---|
| click an outlined node | unlock it |
| right click an unlocked node | lock it again (respec) |
| **Reset** | start over |

**This view is also the answer to "where do node positions live?"** — here, in
the view, and nowhere else. A graph is pure topology with no geometry, which is
what lets one kit do a skill tree, a metro map and a dialogue tree.

So the view works the layout out itself: a node's **column** is how many steps
it is from a starting node (asked via the kit's own `FindPath`), and its **row**
is its position among the nodes at that depth. Tidy left-to-right tree, zero
authored positions, and it re-lays-out automatically when you add a node.

If automatic layout doesn't suit you, the file's header comment lists the two
alternatives — scene GameObjects holding a `GraphNode` reference, or a `Vector2`
on the node asset.

## Adventure view

Page text, then the options. **Locked options are shown greyed out with their
reason** — that's the piece worth copying into your real UI, because letting the
player see what they *could* have done is most of the appeal.

The side panel lists your flags and numbers as they change. Set a pool in the
second slot and a **Random event** button appears.

## Timeline view

| | |
|---|---|
| click a palette button | add that action to the end |
| click a segment on the bar | remove it |
| Play / Pause / Stop | the obvious |
| **Step** | turn-based mode: advance one action |
| 0.5x / 1x / 2x | speed (real-time only — Step ignores it) |
| Loop | start over at the end |

This is the diagram from the guide, live: stacked segments, gaps drawn hollow,
a playhead walking along, and the signals printed as they arrive. The orange
segment is a `waitForFinish` action parking the timeline — a **Finish current**
button appears while it's parked.

Worth doing: press Play, then Stop, then **Step** repeatedly. Same timeline,
same actions, completely different feel. That's the two-drive-modes idea made
tangible.

## Why OnGUI and not proper Unity UI?

Deliberate. `OnGUI` is Unity's old immediate-mode UI — the wrong tool for a real
game and the right tool for this:

- **No setup.** No Canvas, no prefabs, no fonts, no packages, no version
  landmines. It just draws.
- **It cannot be mistaken for your real UI.** So you won't build on it and then
  find you can't take a kit update because you've edited the view.
- **It's short enough to read in one sitting.**

All five use fixed `GUI.*` rectangles rather than `GUILayout`, which sidesteps
the classic "layout mismatch" error beginners hit with OnGUI.

## Moving to a real UI later

Three things change — and none of them is in the kit:

1. **Drawing.** `Box(rect, colour)` becomes an `Image` or a `VisualElement`.
2. **Hit testing.** The screen-to-square maths becomes an `EventSystem` raycast,
   or stays exactly as it is — the maths is correct either way.
3. **Object lifetime.** Instead of redrawing everything each frame, you spawn an
   object per card/node/tile and update it.

**What does not change: a single line of any kit.** All five view files only
*read* their kit and call its public methods. That's the argument for keeping
logic and presentation apart, made concrete rather than asserted.

## One habit worth copying

Every view recalculates rather than patching:

```csharp
report = GridEvaluator.Evaluate(grid);   // after ANY change
```

Don't update totals incrementally as things move. One piece moving can change
three others' bonuses through an aura, and incremental updates are exactly where
this kind of code rots. At the size a human can arrange, recalculating is free.

## Verified

All five views compile clean in both editor and player builds. 33 automated
tests cover the logic that is genuinely new in the view layer:

- **Screen position to board square**, including the off-by-one trap where a
  click one pixel outside the left edge must **not** register as square 0. A
  plain `(int)` cast gets this wrong (it truncates toward zero); `Mathf.FloorToInt`
  gets it right. That bug presents as "sometimes it places in the wrong spot"
  and is horrible to find, so there's a test named after it.
- **Graph layout**, with every node's depth checked against a hand-worked tree.
- **Which targeting modes wait for a click** and which resolve immediately.

Adding these changed nothing in the kits: all 517 kit tests still pass, and the
kit files are byte-identical.
