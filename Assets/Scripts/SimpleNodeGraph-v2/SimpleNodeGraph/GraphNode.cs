using System.Collections.Generic;
using UnityEngine;

// One node in the graph. You make one asset file per node.
//
// A node only knows ONE thing: which other nodes it is connected to.
// That is the whole trick. A skill tree, a world map and a dialogue tree are
// all just "things with a list of neighbours".
//
// To make a skill tree, write your own script that inherits from this one and
// adds whatever a skill actually needs:
//
//     using UnityEngine;
//
//     [CreateAssetMenu(menuName = "Graph/Skill Node")]
//     public class SkillNode : GraphNode
//     {
//         public int cost;
//         public Sprite icon;
//         public string description;
//     }
//
// Same idea for a location (scene name, music) or a dialogue line (speaker, text).

[CreateAssetMenu(menuName = "Graph/Node")]
public class GraphNode : ScriptableObject
{
    [Tooltip("Name shown in your UI. If you leave this empty, the asset's file name is used.")]
    public string title;

    [Tooltip("The nodes this one connects to. Drag other node assets into this list.\n\n" +
             "These connections are ONE WAY. If you want the player to be able to go both " +
             "directions (like a world map), add the connection on both nodes.")]
    public List<GraphNode> connectedTo = new List<GraphNode>();

    // A permanent random ID, created once when you make the asset and then never
    // changed. Save files store this instead of the asset's name, so you are free
    // to rename your nodes whenever you like without breaking anybody's save.
    //
    // It is hidden in the Inspector on purpose - there is no reason to touch it.
    [SerializeField, HideInInspector]
    private string id;

    // Small helper so your UI never shows a blank label.
    public string GetTitle()
    {
        if (string.IsNullOrEmpty(title))
        {
            return name; // "name" is the asset's file name, Unity gives us this for free
        }
        return title;
    }

    // The ID used by saving and loading. You will rarely call this yourself.
    public string GetId()
    {
        // Normally OnValidate has already filled this in. This is just a safety net.
        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();
        }
        return id;
    }

    // Unity calls this in the editor when the asset is created or edited.
    // That is where new nodes get their ID.
    //
    // NOTE: if you add your own OnValidate to your subclass, write it like this so
    // the ID still gets created:
    //
    //     protected override void OnValidate()
    //     {
    //         base.OnValidate();
    //         // ...your own checks here
    //     }
    //
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();

#if UNITY_EDITOR
            // Tells Unity "this asset changed, please write it to disk".
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    // Duplicating an asset with Ctrl+D copies its ID too, which would make two
    // nodes look like the same node to your save file. "Collect Nodes" on your
    // NodeGraph asset spots that and tells you to run this:
    // right-click the node asset and choose "Assign New Id".
    [ContextMenu("Assign New Id")]
    private void AssignNewId()
    {
        id = MakeNewId();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log("Gave " + name + " a fresh ID. Note: any old save file loses this node.");
    }

    // A random string like "9f2c4a17e8b34d0e91c3..." that will never repeat.
    private static string MakeNewId()
    {
        return System.Guid.NewGuid().ToString("N");
    }
}
