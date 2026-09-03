using System.Collections.Generic;
using UnityEngine;

// ONE KIND OF CARD: Strike, Defend, Fireball.
//
// You make one asset per kind, not per copy. A deck with three Strikes in it
// has ONE Strike asset and THREE CardInstances - see CardInstance.cs.
//
// Like the other kits, this asset does not know how to DO anything. It says
// what the card costs, what it needs you to point at, and where it goes
// afterwards. Your own code does the damage when the card is played.
//
// Inherit from it to add your own data:
//
//     using UnityEngine;
//
//     [CreateAssetMenu(menuName = "Cards/Attack Card")]
//     public class AttackCard : Card
//     {
//         public int damage;
//         public GameObject hitEffect;
//     }

[CreateAssetMenu(menuName = "Cards/Card")]
public class Card : ScriptableObject
{
    // What the player has to point at before this card can be played.
    public enum Targeting
    {
        None,       // just play it - a block, a heal on yourself
        ChooseOne,  // pick one thing
        ChooseMany, // pick exactly targetsToChoose things
        All,        // hits everything of the right kind, nothing to pick
    }

    // Where the card goes once it has been played.
    public enum AfterPlay
    {
        Discard,      // the usual
        Exile,        // gone for the rest of the run
        BackToHand,   // it stays in your hand
        ToDrawPile,   // shuffled back in to be drawn again
    }

    [Tooltip("Name shown on the card. If empty, the asset's file name is used.")]
    public string displayName = "";

    [TextArea(2, 5)]
    [Tooltip("The rules text the player reads.")]
    public string description = "";

    [Tooltip("Optional art for your UI. The kit itself never looks at this.")]
    public Sprite art;

    [Tooltip("Energy this costs to play. Set the table's useEnergy to false if " +
             "your game has no energy at all.")]
    public int cost = 1;

    [Header("Targeting")]

    [Tooltip("What the player must point at before this can be played.")]
    public Targeting targeting = Targeting.None;

    [Tooltip("Which kind of thing it targets: \"enemy\", \"ally\", \"card\". " +
             "Your own code decides what these words mean - the kit just matches them.")]
    public string targetKind = "enemy";

    [Tooltip("Only used by ChooseMany: how many things must be picked.")]
    public int targetsToChoose = 2;

    [Header("After playing")]

    [Tooltip("Where this card goes once it has been played.")]
    public AfterPlay afterPlay = AfterPlay.Discard;

    [Header("Labels")]

    [Tooltip("Labels your own code and other cards can look for: " +
             "\"attack\", \"skill\", \"curse\".")]
    public List<string> tags = new List<string>();

    [Tooltip("Optional. Your own code can switch on this instead of comparing " +
             "asset references. Same escape hatch as the other kits.")]
    public string customTag = "";

    // Same permanent hidden ID as the other kits, so saved decks survive
    // renaming and moving your card assets.
    [SerializeField, HideInInspector]
    private string id;

    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return name; // the asset's file name
        }
        return displayName;
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

    public bool NeedsATarget()
    {
        return targeting != Targeting.None;
    }

    // How many the player has to pick. All and None need nothing picked - with
    // All, the kit works out the targets itself.
    public int HowManyToChoose()
    {
        if (targeting == Targeting.ChooseOne) return 1;

        if (targeting == Targeting.ChooseMany)
        {
            return (targetsToChoose < 1) ? 1 : targetsToChoose;
        }

        return 0;
    }

    public string GetId()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();
        }
        return id;
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
        if (cost < 0) cost = 0;
        if (targetsToChoose < 1) targetsToChoose = 1;

        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    // Ctrl+D copies the hidden ID too. The library's problem checker spots that.
    [ContextMenu("Assign New Id")]
    private void AssignNewId()
    {
        id = MakeNewId();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log("Gave " + name + " a fresh ID. Any saved deck loses this card.");
    }

    private static string MakeNewId()
    {
        return System.Guid.NewGuid().ToString("N");
    }
}
