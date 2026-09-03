using System.Collections.Generic;
using UnityEngine;

// ONE KIND OF PIECE: a sword, a shield, a power generator, an L-tetromino.
//
// You make one asset per kind of piece, not per copy - a bag holding three
// swords has one SwordPiece asset and three PlacedPieces on the board.
//
// Inherit from it to add your own data:
//
//     using UnityEngine;
//
//     [CreateAssetMenu(menuName = "Grid/Weapon Piece")]
//     public class WeaponPiece : GridPiece
//     {
//         public int damage;
//         public AudioClip swingSound;
//     }

[CreateAssetMenu(menuName = "Grid/Piece")]
public class GridPiece : ScriptableObject
{
    [Tooltip("Name shown in your UI. If empty, the asset's file name is used.")]
    public string displayName = "";

    [Tooltip("Optional icon for your UI. The kit itself never looks at this.")]
    public Sprite icon;

    [TextArea(4, 10)]
    [Tooltip("DRAW THE SHAPE HERE, one row per line.\n\n" +
             "Use X (or # or O) for a filled square and . for an empty one:\n\n" +
             ".X.\n" +
             "XXX\n\n" +
             "Rows read top to bottom. Rotations are worked out for you.")]
    public string shapeMask = "X";

    [Tooltip("Labels other pieces' rules can look for: \"weapon\", \"food\", \"power\". " +
             "A piece can have as many as you like.")]
    public List<string> tags = new List<string>();

    [Tooltip("What this piece is worth on its own, before any neighbours are " +
             "taken into account.")]
    public List<StatAmount> baseStats = new List<StatAmount>();

    [Tooltip("The 'look around me' rules. This is where the interesting design lives.")]
    public List<AdjacencyRule> rules = new List<AdjacencyRule>();

    // Same permanent hidden ID as the other kits, so save files survive renaming.
    [SerializeField, HideInInspector]
    private string id;

    // The parsed shape. Worked out once, then kept, because parsing text and
    // building four rotations on every single fit-test would be wasteful.
    private PieceShape cachedShape;

    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return name; // the asset's file name
        }
        return displayName;
    }

    public PieceShape GetShape()
    {
        if (cachedShape == null)
        {
            cachedShape = PieceShape.FromMask(shapeMask);
        }
        return cachedShape;
    }

    // Call this if you ever change shapeMask while the game is running.
    public void RefreshShape()
    {
        cachedShape = null;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        foreach (string mine in tags)
        {
            if (mine == tag) return true;
        }

        return false;
    }

    public string GetId()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();
        }
        return id;
    }

    // ScriptableObjects survive between play sessions in the editor, so throw
    // the cached shape away whenever the asset is reloaded.
    private void OnEnable()
    {
        cachedShape = null;
    }

    // Unity calls this in the editor when the asset is created or edited.
    //
    // If you subclass and add your own OnValidate, write it as:
    //
    //     protected override void OnValidate()
    //     {
    //         base.OnValidate();
    //         // ...your own checks
    //     }
    //
    protected virtual void OnValidate()
    {
        // The shape text may have just changed, so parse it again next time.
        cachedShape = null;

        // A distance below 1 would mean "an aura that reaches nothing", which is
        // never what someone meant to type.
        foreach (AdjacencyRule rule in rules)
        {
            if (rule != null && rule.distance < 1) rule.distance = 1;
        }

        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    // Ctrl+D copies the hidden ID too. The board's problem checker spots that.
    [ContextMenu("Assign New Id")]
    private void AssignNewId()
    {
        id = MakeNewId();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log("Gave " + name + " a fresh ID. Any old save file loses this piece.");
    }

    // Right-click the asset to see the shape and all its rotations in the Console.
    // The quickest way to check you drew what you meant to.
    [ContextMenu("Print Shape And Rotations")]
    private void PrintShape()
    {
        RefreshShape();
        PieceShape shape = GetShape();

        string output = GetDisplayName() + ": " + shape.CellCount + " squares, "
                        + shape.RotationCount + " rotation(s)";

        for (int r = 0; r < shape.RotationCount; r++)
        {
            output += "\n\nrotation " + r + " ("
                      + shape.GetWidth(r) + "x" + shape.GetHeight(r) + "):\n"
                      + shape.ToMask(r);
        }

        Debug.Log(output, this);
    }

    private static string MakeNewId()
    {
        return System.Guid.NewGuid().ToString("N");
    }
}
