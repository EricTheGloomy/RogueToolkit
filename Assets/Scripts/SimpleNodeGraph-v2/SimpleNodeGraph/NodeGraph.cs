using System.Collections.Generic;
using UnityEngine;

// The single asset your game points at. It holds the list of every node,
// plus which nodes the player starts with.
//
// Make one of these per tree: SkillTree, WorldMap, Chapter1Dialogue, and so on.

[CreateAssetMenu(menuName = "Graph/Node Graph")]
public class NodeGraph : ScriptableObject
{
    [Tooltip("Nodes the player has from the very start: the free basic skills, the " +
             "home village, the first line of dialogue.")]
    public List<GraphNode> startingNodes = new List<GraphNode>();

    [Tooltip("Every node in this graph. You do not have to fill this in by hand - " +
             "right-click this asset in the Project window and pick 'Collect Nodes'.")]
    public List<GraphNode> allNodes = new List<GraphNode>();

    // Call this when a new game starts. Gives you a fresh GraphState with the
    // starting nodes already unlocked.
    public GraphState CreateNewState()
    {
        GraphState state = new GraphState();

        foreach (GraphNode node in startingNodes)
        {
            state.Unlock(node);
        }

        return state;
    }

    // Used by GraphState.Load to turn a saved ID back into a real node.
    public GraphNode FindNodeById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (GraphNode node in allNodes)
        {
            if (node != null && node.GetId() == id)
            {
                return node;
            }
        }

        return null; // not found - probably a node you deleted since the save
    }

    // Handy for testing and debug commands. Not used by saving, because asset
    // names can change and IDs cannot.
    public GraphNode FindNodeByName(string nodeName)
    {
        foreach (GraphNode node in allNodes)
        {
            if (node != null && node.name == nodeName)
            {
                return node;
            }
        }

        return null; // not found
    }

    // ---------------- finding a route ----------------

    // Finds the shortest route from one node to another, counted in steps.
    // Returns a list that STARTS with "from" and ENDS with "to".
    // Returns null if there is no route at all.
    //
    //   List<GraphNode> route = graph.FindPath(currentTown, targetTown);
    //
    //   if (route == null)  Debug.Log("No way to get there");
    //   else                Debug.Log("It takes " + (route.Count - 1) + " steps");
    //
    // The last argument is optional. Pass your GraphState to only travel through
    // nodes the player has already unlocked, which is how you do "you can only
    // walk through regions you have discovered":
    //
    //   List<GraphNode> route = graph.FindPath(here, there, state);
    //
    public List<GraphNode> FindPath(GraphNode from, GraphNode to, GraphState onlyThroughUnlocked = null)
    {
        if (from == null || to == null) return null;

        // If either end is off-limits there is no point even looking.
        if (onlyThroughUnlocked != null)
        {
            if (!onlyThroughUnlocked.IsUnlocked(from)) return null;
            if (!onlyThroughUnlocked.IsUnlocked(to)) return null;
        }

        // Already there.
        if (from == to)
        {
            List<GraphNode> singleStep = new List<GraphNode>();
            singleStep.Add(from);
            return singleStep;
        }

        // These two lists are used as a PAIR: cameFrom[i] is the node we arrived
        // at visited[i] from. That is how we retrace our steps at the end.
        List<GraphNode> visited = new List<GraphNode>();
        List<GraphNode> cameFrom = new List<GraphNode>();

        // The to-do list, same idea as in CollectNodes below.
        List<GraphNode> toCheck = new List<GraphNode>();

        visited.Add(from);
        cameFrom.Add(null); // we did not come from anywhere, this is the start
        toCheck.Add(from);

        // Because we always take the OLDEST node off the to-do list, we spread out
        // evenly in all directions. That is what makes the first route we find the
        // shortest one.
        while (toCheck.Count > 0)
        {
            GraphNode current = toCheck[0];
            toCheck.RemoveAt(0);

            foreach (GraphNode neighbour in current.connectedTo)
            {
                if (neighbour == null) continue;                 // empty slot in the list
                if (visited.Contains(neighbour)) continue;       // been there
                if (onlyThroughUnlocked != null && !onlyThroughUnlocked.IsUnlocked(neighbour)) continue;

                visited.Add(neighbour);
                cameFrom.Add(current);

                // Got there. Stop and work out the route we took.
                if (neighbour == to)
                {
                    return BuildRoute(to, visited, cameFrom);
                }

                toCheck.Add(neighbour);
            }
        }

        return null; // checked everything we could reach, never found it
    }

    // Walks backwards from the destination using the cameFrom list, then flips
    // the result around so it reads start-to-finish.
    private List<GraphNode> BuildRoute(GraphNode destination, List<GraphNode> visited, List<GraphNode> cameFrom)
    {
        List<GraphNode> route = new List<GraphNode>();

        GraphNode step = destination;

        while (step != null)
        {
            route.Add(step);

            int index = visited.IndexOf(step); // where in the visited list is this node?
            step = cameFrom[index];            // and what did we arrive from?
        }

        route.Reverse(); // we collected it end-to-start, so turn it around
        return route;
    }

    // ---------------- editor helper ----------------

    // Right-click this asset in the Project window and choose "Collect Nodes".
    // It starts at the starting nodes, follows every connection, and fills in
    // allNodes for you. Run it again whenever you add or remove nodes.
    [ContextMenu("Collect Nodes")]
    public void CollectNodes()
    {
        allNodes.Clear();

        // A to-do list of nodes we still have to look at.
        List<GraphNode> toCheck = new List<GraphNode>();

        foreach (GraphNode node in startingNodes)
        {
            if (node != null)
            {
                toCheck.Add(node);
            }
        }

        // Keep going until the to-do list is empty.
        while (toCheck.Count > 0)
        {
            // Take the first node off the to-do list.
            GraphNode current = toCheck[0];
            toCheck.RemoveAt(0);

            // Skip it if we already dealt with it (stops us looping forever
            // when two nodes point at each other).
            if (allNodes.Contains(current)) continue;

            allNodes.Add(current);

            // Add its neighbours to the to-do list.
            foreach (GraphNode neighbour in current.connectedTo)
            {
                if (neighbour != null)
                {
                    toCheck.Add(neighbour);
                }
            }
        }

        Debug.Log("Collect Nodes: found " + allNodes.Count + " nodes in " + name);

        CheckForDuplicateIds();

#if UNITY_EDITOR
        // Tells Unity "this asset changed, please save it".
        // Without this, the list you just filled in can be lost on restart.
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // Duplicating a node asset with Ctrl+D copies its hidden ID as well, so two
    // different nodes end up looking identical to your save file. That is the one
    // way to break ID-based saving, so we check for it here and tell you exactly
    // what to do about it.
    private void CheckForDuplicateIds()
    {
        // Compare every node against every node after it in the list.
        for (int i = 0; i < allNodes.Count; i++)
        {
            for (int j = i + 1; j < allNodes.Count; j++)
            {
                if (allNodes[i].GetId() == allNodes[j].GetId())
                {
                    Debug.LogError(
                        "Two nodes share the same hidden ID: '" + allNodes[i].name +
                        "' and '" + allNodes[j].name + "'. This happens when you " +
                        "duplicate a node with Ctrl+D. Fix it by right-clicking '" +
                        allNodes[j].name + "' and choosing 'Assign New Id'.");
                }
            }
        }
    }
}
