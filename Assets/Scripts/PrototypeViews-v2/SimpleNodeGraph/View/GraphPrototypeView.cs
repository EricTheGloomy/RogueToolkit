using System.Collections.Generic;
using UnityEngine;

// A THROWAWAY VIEW for the SimpleNodeGraph kit.
//
// HOW TO USE: add this to any empty GameObject and press Play. That is all.
//
//   click an outlined node    unlock it
//   right click an unlocked   lock it again (respec)
//   Reset button              start over
//
//
// THIS FILE IS ALSO THE ANSWER TO "WHERE DO NODE POSITIONS LIVE?"
//
// They live HERE, in the view, and nowhere else. A graph is pure topology -
// "A connects to B" - with no geometry at all. That is what lets the same kit
// do a skill tree, a metro map and a dialogue tree.
//
// So the view has to decide where things go. This one works it out
// automatically: a node's COLUMN is how many steps it is from a starting node
// (asked via FindPath), and its ROW is just its position among the nodes at
// that same depth. Tidy left-to-right tree, zero authored positions.
//
// Two other ways you could do it, if automatic layout does not suit:
//   - put GameObjects in your scene, each holding a reference to its GraphNode,
//     and read transform.position. Best for a hand-drawn map.
//   - add a Vector2 to GraphNode. Only worth it when the layout is content in
//     its own right.
//
// Delete this file when you are done with it.

public class GraphPrototypeView : MonoBehaviour
{
    [Tooltip("Leave empty to use the built-in demo skill tree.")]
    public NodeGraph graph;

    [Header("Look")]
    public int nodeWidth = 118;
    public int nodeHeight = 40;
    public int columnGap = 60;
    public int rowGap = 16;
    public int left = 40;
    public int top = 70;

    private GraphState state;
    private string message = "";
    private Texture2D dot;

    // Worked out once in Start: where each node sits.
    private Dictionary<GraphNode, Rect> layout = new Dictionary<GraphNode, Rect>();

    private static readonly Color unlockedColour = new Color(0.24f, 0.55f, 0.52f);
    private static readonly Color availableColour = new Color(0.20f, 0.32f, 0.33f);
    private static readonly Color availableEdge = new Color(0.42f, 0.82f, 0.78f);
    private static readonly Color lockedColour = new Color(0.20f, 0.21f, 0.24f);
    private static readonly Color lineColour = new Color(0.35f, 0.37f, 0.41f);
    private static readonly Color background = new Color(0.11f, 0.12f, 0.14f);

    void Start()
    {
        if (graph == null) graph = BuildDemoTree();

        state = graph.CreateNewState();
        BuildLayout();
        message = "Click an outlined node to unlock it.";
    }

    // ---------------- working out where everything goes ----------------

    void BuildLayout()
    {
        layout.Clear();

        // Group the nodes by how far they are from a starting node.
        Dictionary<int, List<GraphNode>> columns = new Dictionary<int, List<GraphNode>>();

        foreach (GraphNode node in graph.allNodes)
        {
            if (node == null) continue;

            int depth = DepthOf(node);

            if (!columns.ContainsKey(depth)) columns[depth] = new List<GraphNode>();
            columns[depth].Add(node);
        }

        // Then hand out rectangles: column by depth, row by order within it.
        foreach (int depth in columns.Keys)
        {
            List<GraphNode> inColumn = columns[depth];

            for (int row = 0; row < inColumn.Count; row++)
            {
                layout[inColumn[row]] = new Rect(
                    left + depth * (nodeWidth + columnGap),
                    top + row * (nodeHeight + rowGap),
                    nodeWidth,
                    nodeHeight);
            }
        }
    }

    // How many steps from the nearest starting node? The kit already knows how
    // to walk the connections, so we just ask it.
    int DepthOf(GraphNode node)
    {
        int best = 99;

        foreach (GraphNode root in graph.startingNodes)
        {
            if (root == null) continue;

            List<GraphNode> route = graph.FindPath(root, node);
            if (route == null) continue;

            int steps = route.Count - 1;
            if (steps < best) best = steps;
        }

        // Not reachable from any root - park it in a column of its own on the
        // far right rather than hiding it.
        return (best == 99) ? 0 : best;
    }

    // ---------------- drawing ----------------

    void OnGUI()
    {
        EnsureDot();
        Box(new Rect(0, 0, Screen.width, Screen.height), background);

        GUI.Label(new Rect(left, 14, 700, 22),
                  "SimpleNodeGraph prototype view - click to unlock, right click to lock again");

        DrawConnections();
        DrawNodes();

        GUI.Label(new Rect(left, Screen.height - 52, 600, 22),
                  "unlocked " + state.unlockedNodes.Count + " of " + graph.allNodes.Count
                  + "    available now: " + state.GetUnlockableNodes().Count);

        GUI.Label(new Rect(left, Screen.height - 30, 700, 22), message);

        if (GUI.Button(new Rect(Screen.width - 130, 14, 110, 26), "Reset"))
        {
            state = graph.CreateNewState();
            message = "Reset.";
        }
    }

    // Elbow connectors: out to the right, across, then in. Three rectangles,
    // no line-drawing needed, and it looks like a skill tree on purpose.
    void DrawConnections()
    {
        foreach (GraphNode node in graph.allNodes)
        {
            if (node == null || !layout.ContainsKey(node)) continue;

            Rect from = layout[node];

            foreach (GraphNode neighbour in node.connectedTo)
            {
                if (neighbour == null || !layout.ContainsKey(neighbour)) continue;

                Rect to = layout[neighbour];

                float startX = from.x + from.width;
                float startY = from.y + from.height / 2f;
                float endX = to.x;
                float endY = to.y + to.height / 2f;
                float midX = (startX + endX) / 2f;

                Box(new Rect(startX, startY - 1, midX - startX, 2), lineColour);
                Box(new Rect(midX - 1, Mathf.Min(startY, endY), 2, Mathf.Abs(endY - startY)), lineColour);
                Box(new Rect(midX, endY - 1, endX - midX, 2), lineColour);
            }
        }
    }

    void DrawNodes()
    {
        Event e = Event.current;

        foreach (GraphNode node in graph.allNodes)
        {
            if (node == null || !layout.ContainsKey(node)) continue;

            Rect area = layout[node];

            bool unlocked = state.IsUnlocked(node);
            bool available = state.CanUnlock(node);

            if (unlocked)
            {
                Box(area, unlockedColour);
            }
            else if (available)
            {
                // Outline by drawing a slightly bigger box behind it.
                Box(new Rect(area.x - 2, area.y - 2, area.width + 4, area.height + 4), availableEdge);
                Box(area, availableColour);
            }
            else
            {
                Box(area, lockedColour);
            }

            GUI.Label(new Rect(area.x + 8, area.y + 10, area.width - 12, 22), node.GetTitle());

            // Clicks. Left unlocks, right locks.
            if (e != null && e.type == EventType.MouseDown && Contains(area, e.mousePosition))
            {
                if (e.button == 0) TryUnlock(node);
                else if (e.button == 1) TryLock(node);

                e.Use();
            }
        }
    }

    // ---------------- the bit you would actually write in your game ----------

    void TryUnlock(GraphNode node)
    {
        if (state.IsUnlocked(node))
        {
            message = node.GetTitle() + " is already unlocked.";
            return;
        }

        if (!state.CanUnlock(node))
        {
            // In a real game you would also check the cost here:
            //   if (points < ((SkillNode)node).cost) ...
            message = node.GetTitle() + " needs one of its prerequisites first.";
            return;
        }

        state.Unlock(node);
        message = "Unlocked " + node.GetTitle() + ".";
    }

    void TryLock(GraphNode node)
    {
        if (state.Lock(node)) message = "Locked " + node.GetTitle() + " again.";
        else message = node.GetTitle() + " was not unlocked.";
    }

    // ---------------- helpers ----------------

    bool Contains(Rect area, Vector2 point)
    {
        return point.x >= area.x && point.x <= area.x + area.width
            && point.y >= area.y && point.y <= area.y + area.height;
    }

    void Box(Rect area, Color colour)
    {
        Color was = GUI.color;
        GUI.color = colour;
        GUI.DrawTexture(area, dot);
        GUI.color = was;
    }

    void EnsureDot()
    {
        if (dot != null) return;
        dot = new Texture2D(1, 1);
        dot.SetPixel(0, 0, Color.white);
        dot.Apply();
    }

    // ---------------- the demo tree ----------------

    NodeGraph BuildDemoTree()
    {
        GraphNode root = MakeNode("Basics");
        GraphNode strength = MakeNode("Strength");
        GraphNode agility = MakeNode("Agility");
        GraphNode heavyArmour = MakeNode("Heavy Armour");
        GraphNode cleave = MakeNode("Cleave");
        GraphNode dodge = MakeNode("Dodge");
        GraphNode evasion = MakeNode("Evasion");
        GraphNode berserk = MakeNode("Berserk");

        Connect(root, strength);
        Connect(root, agility);
        Connect(strength, heavyArmour);
        Connect(strength, cleave);
        Connect(agility, dodge);
        Connect(dodge, evasion);
        Connect(cleave, berserk);

        NodeGraph built = ScriptableObject.CreateInstance<NodeGraph>();
        built.name = "DemoTree";
        built.startingNodes.Add(root);
        built.CollectNodes();
        return built;
    }

    GraphNode MakeNode(string nodeName)
    {
        GraphNode node = ScriptableObject.CreateInstance<GraphNode>();
        node.name = nodeName;
        node.title = nodeName;
        return node;
    }

    void Connect(GraphNode from, GraphNode to)
    {
        from.connectedTo.Add(to);
    }
}
