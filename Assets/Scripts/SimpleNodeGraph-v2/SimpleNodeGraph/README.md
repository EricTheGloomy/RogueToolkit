# Simple Node Graph for Unity

Version 2 — 31 August 2026. This bundle replaces every earlier file from our
conversation. If you have loose copies of these scripts lying around, delete
them and use these.

## What's in here

| File | What it is |
|---|---|
| `GraphNode.cs` | One node: a title, a list of neighbours, a hidden ID. **Required.** |
| `GraphState.cs` | The player's progress: which nodes are unlocked. **Required.** |
| `NodeGraph.cs` | The graph asset. Holds all nodes, starts new games, finds routes. **Required.** |
| `GraphExample.cs` | A throwaway test script that prints to the Console. Delete once it makes sense. |
| `HowToUse.html` | The full guide. Open it in a browser. Not a Unity file — see below. |

## Install

1. Copy the four `.cs` files into your project, anywhere under `Assets`.
2. **Move `HowToUse.html` and `README.md` out of `Assets`** — Unity doesn't care
   about them, but there's no reason to have them in your project. Keep them
   next to the project folder, or just bookmark the online version.
3. Let Unity compile. No packages, no namespaces, no setup.

## The 60-second version

A node is a ScriptableObject asset with a `List<GraphNode> connectedTo`.
That list **is** the graph. There is no clever graph class anywhere.

```csharp
// Start a new game
GraphState state = myGraph.CreateNewState();

// What can the player unlock right now?
List<GraphNode> options = state.GetUnlockableNodes();

// Is one specific node available?
if (state.CanUnlock(node)) state.Unlock(node);

// Route from A to B (world maps). Pass state to only use unlocked nodes.
List<GraphNode> route = myGraph.FindPath(from, to, state);

// Save and load
List<string> ids = state.Save();
state.Load(ids, myGraph);
```

## First-time setup in Unity

1. Project window: **right-click → Create → Graph → Node**. Make three:
   `Root`, `Strength`, `HeavyArmor`.
2. Select `Root`, drag `Strength` into its **Connected To** list.
   Select `Strength`, drag `HeavyArmor` into its list.
3. **right-click → Create → Graph → Node Graph**. Drag `Root` into its
   **Starting Nodes**, then right-click the asset and pick **Collect Nodes**.
   The Console should say "found 3 nodes".
4. Empty GameObject → add the **Graph Example** component → drag the Node Graph
   asset into its slot → press Play → read the Console.

## Two things that will bite you

**Never put progress on a node.** Adding `public bool unlocked` to `GraphNode`
is the obvious move and it breaks two ways: Unity writes the value into the
asset file while you playtest, so your tree stays half-unlocked next time you
press Play, and every save slot shares one value. That's why `GraphState` is a
separate object.

**Ctrl+D copies a node's hidden ID.** Duplicating an asset copies its ID too,
so two nodes look identical to your save file. **Collect Nodes** detects this
and logs an error naming both assets; the fix is to right-click the copy and
choose **Assign New Id**. So after duplicating nodes, re-run Collect Nodes and
glance at the Console.

## Making it yours

Inherit from `GraphNode` and add whatever your nodes actually need:

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Graph/Skill Node")]
public class SkillNode : GraphNode
{
    public int cost = 1;
    public Sprite icon;
    public string description;
}
```

Now create **Skill Node** assets instead of plain **Node** assets. Everything
still works, because a `SkillNode` *is* a `GraphNode`.

If you ever add your own `OnValidate` to a subclass, write it like this or your
nodes will never get an ID:

```csharp
protected override void OnValidate()
{
    base.OnValidate();
    // ...your own checks
}
```

`HowToUse.html` has worked recipes for skill trees, world maps and dialogue,
plus the complete list of everything you can call.

## Verified

57 automated tests pass, covering: unlocking and prerequisite rules, respec,
save/load, surviving asset renames, duplicate-ID detection, empty Inspector
slots, one-way and two-way connections, and pathfinding (shortest route,
dead ends, and routes limited to unlocked nodes). Compiles clean in both
editor and player builds.
