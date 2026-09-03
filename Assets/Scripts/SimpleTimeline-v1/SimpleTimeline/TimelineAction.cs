using UnityEngine;

// ONE THING THAT CAN HAPPEN. A card, an attack, a building order, a spawn wave,
// a camera move, a line of dialogue - anything.
//
// Important: this asset does NOT know how to do the thing. It only says
// "this exists, it is called X, and it takes Y long". Your own code does the
// actual work when the runner tells you the action started.
//
// That split is the whole reason one small kit works for cards AND attacks AND
// production queues: the timeline only ever deals with ORDER and LENGTH.
//
// Inherit from it to add your own data:
//
//     using UnityEngine;
//
//     [CreateAssetMenu(menuName = "Timeline/Attack")]
//     public class AttackAction : TimelineAction
//     {
//         public int damage;
//         public GameObject effectPrefab;
//     }

[CreateAssetMenu(menuName = "Timeline/Action")]
public class TimelineAction : ScriptableObject
{
    [Tooltip("Name shown in your UI. If empty, the asset's file name is used.")]
    public string displayName = "";

    [Tooltip("How much room this takes up on the timeline.\n\n" +
             "Real-time timeline: this is SECONDS.\n" +
             "Turn-based timeline: leave everything at 1 and use StepOne() - " +
             "the number stops mattering.\n\n" +
             "0 means instant: it starts and finishes in the same moment.")]
    public float duration = 1f;

    [Tooltip("ON: the timeline stops dead the moment this action starts, and waits " +
             "until your code calls runner.FinishCurrent().\n\n" +
             "Use it when the real length is not a number you know up front: an " +
             "animation, a player confirmation, a server reply.")]
    public bool waitForFinish = false;

    [Tooltip("Optional. Your own code can switch on this instead of comparing asset " +
             "references. Same escape hatch as the adventure kit.")]
    public string customTag = "";

    // Same permanent hidden ID as the other kits, for the same reason: save files
    // store this, so you can rename and move your action assets freely.
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

    // Never negative, even if someone types -3 in the Inspector. A negative
    // length would run the playhead backwards and break everything after it.
    public float GetDuration()
    {
        if (duration < 0f) return 0f;

        return duration;
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
        if (duration < 0f) duration = 0f;

        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    // Ctrl+D copies the hidden ID too. The library's "Check For Problems"
    // spots that and points you here.
    [ContextMenu("Assign New Id")]
    private void AssignNewId()
    {
        id = MakeNewId();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log("Gave " + name + " a fresh ID. Any old save file loses this action.");
    }

    private static string MakeNewId()
    {
        return System.Guid.NewGuid().ToString("N");
    }
}
