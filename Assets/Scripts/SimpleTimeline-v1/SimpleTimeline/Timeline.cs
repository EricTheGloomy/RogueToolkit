using System.Collections.Generic;
using UnityEngine;

// THE ORDERED LIST. This is what the player builds by dragging cards into slots,
// or what you fill in from a preset.
//
// It is a plain class, not a ScriptableObject, because it changes constantly
// while the player is arranging things. (Same reason as the other kits: putting
// changing data on an asset means Unity writes it into the file while you
// playtest, and every save slot shares it.)
//
// The timeline knows nothing about what the actions DO. It only knows the order
// they are in and how long each one takes.

public class Timeline
{
    // One placed thing on the timeline.
    public class Entry
    {
        public TimelineAction action;

        // Dead air BEFORE this entry. Leave it at 0 and entries run back to
        // back; set it to make a gap in a scripted sequence.
        public float delayBefore = 0f;

        public Entry(TimelineAction action)
        {
            this.action = action;
        }

        public Entry(TimelineAction action, float delayBefore)
        {
            this.action = action;
            this.delayBefore = delayBefore;
        }

        // How much room this entry takes in total: its gap plus its action.
        public float GetLength()
        {
            return GetDelay() + GetActionDuration();
        }

        public float GetDelay()
        {
            if (delayBefore < 0f) return 0f;

            return delayBefore;
        }

        public float GetActionDuration()
        {
            if (action == null) return 0f;

            return action.GetDuration();
        }
    }

    // Most entries you can place. 0 means no limit.
    public int maxEntries = 0;

    // Most total length you can place - think "action points". 0 means no limit.
    public float maxTotalDuration = 0f;

    private List<Entry> entries = new List<Entry>();

    // ---------------- reading ----------------

    public int Count
    {
        get { return entries.Count; }
    }

    public Entry GetEntry(int index)
    {
        if (index < 0 || index >= entries.Count) return null;

        return entries[index];
    }

    public TimelineAction GetAction(int index)
    {
        Entry entry = GetEntry(index);
        if (entry == null) return null;

        return entry.action;
    }

    // A copy, so nobody can reorder our list behind our back by accident.
    public List<Entry> GetAllEntries()
    {
        return new List<Entry>(entries);
    }

    // ---------------- editing ----------------

    // Would this fit? Use it to grey out the buttons in your palette so the
    // player can see they are full before they click.
    public bool CanAdd(TimelineAction action)
    {
        if (action == null) return false;

        if (maxEntries > 0 && entries.Count >= maxEntries) return false;

        if (maxTotalDuration > 0f)
        {
            if (GetTotalDuration() + action.GetDuration() > maxTotalDuration) return false;
        }

        return true;
    }

    // Puts an action on the end. Returns false if it did not fit.
    public bool Add(TimelineAction action)
    {
        return Insert(entries.Count, action, 0f);
    }

    public bool Add(TimelineAction action, float delayBefore)
    {
        return Insert(entries.Count, action, delayBefore);
    }

    // Puts an action at a position, pushing everything after it along.
    // An index past the end just goes on the end, which is what a drag-and-drop
    // UI usually wants.
    public bool Insert(int index, TimelineAction action, float delayBefore)
    {
        if (!CanAdd(action)) return false;

        if (index < 0) index = 0;
        if (index > entries.Count) index = entries.Count;

        entries.Insert(index, new Entry(action, delayBefore));
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= entries.Count) return false;

        entries.RemoveAt(index);
        return true;
    }

    // Drag-to-reorder. Moves the entry at 'from' so it ends up at 'to'.
    public bool Move(int from, int to)
    {
        if (from < 0 || from >= entries.Count) return false;
        if (to < 0 || to >= entries.Count) return false;
        if (from == to) return true; // nothing to do, but not a failure

        Entry moving = entries[from];
        entries.RemoveAt(from);
        entries.Insert(to, moving);
        return true;
    }

    public void Clear()
    {
        entries.Clear();
    }

    // ---------------- timing ----------------
    //
    // Nothing is stored: start and end times are worked out by stacking the
    // entries up. That means reordering or removing something automatically
    // re-times everything after it, with no bookkeeping to get wrong.

    public float GetTotalDuration()
    {
        float total = 0f;

        foreach (Entry entry in entries)
        {
            total += entry.GetLength();
        }

        return total;
    }

    // When does this entry's action begin?
    public float GetStartTime(int index)
    {
        if (index < 0 || index >= entries.Count) return 0f;

        float time = 0f;

        // Everything before it takes up its full length...
        for (int i = 0; i < index; i++)
        {
            time += entries[i].GetLength();
        }

        // ...then this entry's own gap comes before its action.
        time += entries[index].GetDelay();

        return time;
    }

    public float GetEndTime(int index)
    {
        Entry entry = GetEntry(index);
        if (entry == null) return 0f;

        return GetStartTime(index) + entry.GetActionDuration();
    }

    // ---------------- saving ----------------

    public TimelineSave Save()
    {
        TimelineSave save = new TimelineSave();

        foreach (Entry entry in entries)
        {
            if (entry.action == null) continue; // an empty slot is not worth saving

            save.actionIds.Add(entry.action.GetId());
            save.delays.Add(entry.delayBefore);
        }

        return save;
    }

    // Rebuilds the list from a save. The library is how an ID becomes a real
    // action again. Anything the library no longer knows about is skipped.
    public void Load(TimelineSave save, TimelineLibrary library)
    {
        entries.Clear();

        if (save == null || library == null) return;
        if (save.actionIds == null) return;

        for (int i = 0; i < save.actionIds.Count; i++)
        {
            TimelineAction action = library.FindActionById(save.actionIds[i]);
            if (action == null) continue;

            float delay = 0f;
            if (save.delays != null && i < save.delays.Count) delay = save.delays[i];

            entries.Add(new Entry(action, delay));
        }
    }
}

// A plain data holder. Hand it straight to JsonUtility:
//
//     string json = JsonUtility.ToJson(timeline.Save());
//     timeline.Load(JsonUtility.FromJson<TimelineSave>(json), library);
//
[System.Serializable]
public class TimelineSave
{
    public List<string> actionIds = new List<string>();
    public List<float> delays = new List<float>();
}
