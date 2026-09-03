using System.Collections.Generic;

// Keeps track of which nodes the player has unlocked.
//
// READ THIS BIT, it is the one thing that catches everybody out:
//
// This is a normal class, NOT a ScriptableObject. That is on purpose.
// You might be tempted to just put "public bool unlocked" on GraphNode instead.
// Do not. Two things go wrong:
//
//   1. Unity writes the value back into the asset file while you playtest, so your
//      skill tree stays half-unlocked the next time you press Play. Very confusing.
//   2. Every save slot would share the same value.
//
// So keep it split:  the nodes are the MAP (never changes),
//                    this class is the PROGRESS (changes all the time).

public class GraphState
{
    // The nodes that are unlocked right now.
    public List<GraphNode> unlockedNodes = new List<GraphNode>();

    public bool IsUnlocked(GraphNode node)
    {
        return unlockedNodes.Contains(node);
    }

    // Unlocks a node. Returns true if something actually changed, so you can do:
    //   if (state.Unlock(node)) { playSound(); }
    public bool Unlock(GraphNode node)
    {
        if (node == null) return false;
        if (unlockedNodes.Contains(node)) return false; // already unlocked, do nothing

        unlockedNodes.Add(node);
        return true;
    }

    public bool Lock(GraphNode node)
    {
        return unlockedNodes.Remove(node); // Remove already returns true/false for us
    }

    public void ResetProgress()
    {
        unlockedNodes.Clear();
    }

    // Is this node connected to something the player already unlocked?
    // This is the normal "you have the prerequisite" rule. Use it before unlocking:
    //
    //   if (state.CanUnlock(node) && playerPoints >= node.cost) { state.Unlock(node); }
    //
    public bool CanUnlock(GraphNode node)
    {
        if (node == null) return false;
        if (unlockedNodes.Contains(node)) return false; // already have it

        // Go through everything we own and see if any of them points at this node.
        foreach (GraphNode owned in unlockedNodes)
        {
            if (owned.connectedTo.Contains(node))
            {
                return true;
            }
        }

        return false;
    }

    // Every node the player could unlock next.
    // Great for lighting up the buttons that are available in your UI.
    public List<GraphNode> GetUnlockableNodes()
    {
        List<GraphNode> result = new List<GraphNode>();

        // For each node we own...
        foreach (GraphNode owned in unlockedNodes)
        {
            // ...look at each of its neighbours.
            foreach (GraphNode neighbour in owned.connectedTo)
            {
                if (neighbour == null) continue;                 // an empty slot in the list
                if (unlockedNodes.Contains(neighbour)) continue; // already unlocked
                if (result.Contains(neighbour)) continue;        // already added it

                result.Add(neighbour);
            }
        }

        return result;
    }

    // ---------------- saving and loading ----------------
    //
    // You cannot put a ScriptableObject reference in a save file, so we save each
    // node's hidden ID as a list of strings and look them back up on load.
    //
    // The IDs never change, so renaming or moving your node assets is completely
    // safe - old save files keep working.

    public List<string> Save()
    {
        List<string> ids = new List<string>();

        foreach (GraphNode node in unlockedNodes)
        {
            ids.Add(node.GetId());
        }

        return ids;
    }

    public void Load(List<string> savedIds, NodeGraph graph)
    {
        unlockedNodes.Clear();

        if (savedIds == null) return;

        foreach (string savedId in savedIds)
        {
            GraphNode node = graph.FindNodeById(savedId);

            // node is null if you deleted it since the save was made, or if the
            // save is from an older version of the game. Skipping is better
            // than crashing.
            if (node != null)
            {
                unlockedNodes.Add(node);
            }
        }
    }
}
