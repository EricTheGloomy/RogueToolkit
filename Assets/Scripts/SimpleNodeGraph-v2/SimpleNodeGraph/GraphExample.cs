using System.Collections.Generic;
using UnityEngine;

// A throwaway test script so you can SEE the thing working.
//
// How to use it:
//   1. Put this on any GameObject in your scene (an empty one is fine).
//   2. Drag your NodeGraph asset into the "graph" slot in the Inspector.
//   3. Press Play and read the Console window.
//
// Delete this file once it makes sense. Nothing else depends on it.

public class GraphExample : MonoBehaviour
{
    public NodeGraph graph;

    private GraphState state;

    void Start()
    {
        if (graph == null)
        {
            Debug.LogError("Drag your NodeGraph asset into the 'graph' slot first.");
            return;
        }

        // --- 1. Start a new game -------------------------------------------
        state = graph.CreateNewState();
        PrintStatus("New game");

        // --- 2. Unlock the first available node ----------------------------
        List<GraphNode> options = state.GetUnlockableNodes();

        if (options.Count > 0)
        {
            GraphNode picked = options[0];

            // In a real game you would also check the player can afford it:
            //   if (state.CanUnlock(picked) && points >= picked.cost)
            if (state.CanUnlock(picked))
            {
                state.Unlock(picked);
                Debug.Log("Unlocked: " + picked.GetTitle());
            }
        }
        else
        {
            Debug.Log("Nothing available. Are your starting nodes connected to anything?");
        }

        PrintStatus("After unlocking one node");

        // --- 3. Save and load, to prove progress survives ------------------
        List<string> savedData = state.Save();

        state.ResetProgress();
        Debug.Log("After wiping progress: " + state.unlockedNodes.Count + " unlocked");

        state.Load(savedData, graph);
        Debug.Log("After loading again:   " + state.unlockedNodes.Count + " unlocked");
    }

    // Prints what is unlocked and what is available next.
    void PrintStatus(string label)
    {
        string unlocked = "";
        foreach (GraphNode node in state.unlockedNodes)
        {
            unlocked += node.GetTitle() + ", ";
        }

        string available = "";
        foreach (GraphNode node in state.GetUnlockableNodes())
        {
            available += node.GetTitle() + ", ";
        }

        Debug.Log(label + "\n   unlocked: " + unlocked + "\n   can unlock next: " + available);
    }
}
