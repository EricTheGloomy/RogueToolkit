using System.Collections.Generic;

// Everything the adventure remembers about the player.
//
// Three kinds of memory, and that is deliberately all:
//
//   FLAGS   on/off facts        "has_rusty_key", "spared_the_wolf"
//   STATS   named numbers       "gold" = 40, "hp" = 12
//   SEEN    events already had  so "only once" random events work
//
// Same rule as always: this is a normal class, NOT a ScriptableObject, so it
// changes freely at runtime and goes into your save file without Unity writing
// anything back into your event assets.

public class AdventureState
{
    private List<string> flags = new List<string>();
    private Dictionary<string, int> stats = new Dictionary<string, int>();
    private List<string> seenEventIds = new List<string>();

    // ---------------- flags: on/off facts ----------------

    public bool HasFlag(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        return flags.Contains(key);
    }

    public void SetFlag(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (!flags.Contains(key))
        {
            flags.Add(key);
        }
    }

    public void ClearFlag(string key)
    {
        flags.Remove(key); // does nothing if it was not there, which is fine
    }

    // ---------------- stats: named numbers ----------------

    // A number you have never set is simply 0. That means you do not have to
    // set anything up before using a new stat name.
    public int GetStat(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;

        int found;
        if (stats.TryGetValue(key, out found))
        {
            return found;
        }

        return 0;
    }

    public void SetStat(string key, int newValue)
    {
        if (string.IsNullOrEmpty(key)) return;

        stats[key] = newValue;
    }

    public void AddToStat(string key, int amount)
    {
        SetStat(key, GetStat(key) + amount);
    }

    // ---------------- which events have already happened ----------------

    public bool HasSeen(AdventureEvent evt)
    {
        if (evt == null) return false;

        return seenEventIds.Contains(evt.GetId());
    }

    public void MarkSeen(AdventureEvent evt)
    {
        if (evt == null) return;

        string eventId = evt.GetId();

        if (!seenEventIds.Contains(eventId))
        {
            seenEventIds.Add(eventId);
        }
    }

    // ---------------- housekeeping ----------------

    public void Clear()
    {
        flags.Clear();
        stats.Clear();
        seenEventIds.Clear();
    }

    // Copies for debug displays. Copies, so nobody can edit our lists behind
    // our back by accident.
    public List<string> GetAllFlags()
    {
        return new List<string>(flags);
    }

    public List<string> GetAllStatNames()
    {
        return new List<string>(stats.Keys);
    }

    // ---------------- saving ----------------

    public AdventureSave Save()
    {
        AdventureSave save = new AdventureSave();

        save.flags = new List<string>(flags);
        save.seenEventIds = new List<string>(seenEventIds);

        // Unity's JsonUtility cannot save a Dictionary, so we split it into two
        // lists that line up with each other: statKeys[3] goes with statValues[3].
        // This is a well-known Unity annoyance, not something we invented.
        save.statKeys = new List<string>();
        save.statValues = new List<int>();

        foreach (string key in stats.Keys)
        {
            save.statKeys.Add(key);
            save.statValues.Add(stats[key]);
        }

        return save;
    }

    public void Load(AdventureSave save)
    {
        Clear();

        if (save == null) return;

        if (save.flags != null)        flags.AddRange(save.flags);
        if (save.seenEventIds != null) seenEventIds.AddRange(save.seenEventIds);

        if (save.statKeys != null && save.statValues != null)
        {
            for (int i = 0; i < save.statKeys.Count; i++)
            {
                // Guard against a truncated or hand-edited save file, where the
                // two lists might not be the same length any more.
                if (i < save.statValues.Count)
                {
                    stats[save.statKeys[i]] = save.statValues[i];
                }
            }
        }
    }
}

// A plain data holder. Hand it straight to JsonUtility:
//
//     string json = JsonUtility.ToJson(runner.Save());
//     AdventureSave loaded = JsonUtility.FromJson<AdventureSave>(json);
//
[System.Serializable]
public class AdventureSave
{
    public List<string> flags = new List<string>();
    public List<string> statKeys = new List<string>();
    public List<int> statValues = new List<int>();
    public List<string> seenEventIds = new List<string>();

    // Which event the player was on, so you can drop them back into a
    // half-finished story. Empty means the adventure was over.
    public string currentEventId = "";
}
