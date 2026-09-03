using System.Collections.Generic;
using UnityEngine;

// A bag of random events. Ask it for one and it picks at random from whatever
// the player is currently eligible for.
//
// Make one per situation: WildernessEvents, TownEvents, BossRoomEvents.

[CreateAssetMenu(menuName = "Adventure/Event Pool")]
public class EventPool : ScriptableObject
{
    // One entry in the bag. Edited inline in the pool's Inspector.
    [System.Serializable]
    public class Entry
    {
        [Tooltip("The event to fire.")]
        public AdventureEvent evt;

        [Tooltip("How likely this is compared to the others in the pool. " +
                 "Weight 3 is three times as likely as weight 1. " +
                 "Zero or less means it never comes up.")]
        public int weight = 1;

        [Tooltip("ALL of these must be true for this event to be possible at all. " +
                 "Use it for events that only make sense sometimes: " +
                 "\"only if the player has a horse\".")]
        public List<Requirement> requirements = new List<Requirement>();

        [Tooltip("ON: once the player has had this event, it never comes up again. " +
                 "Good for one-off story beats hidden among the repeatable filler.")]
        public bool onlyOnce = false;
    }

    public List<Entry> entries = new List<Entry>();

    // Picks a random event the player is eligible for right now.
    // Returns null if nothing in the pool is possible - always check for that,
    // it is completely normal once the one-off events are used up.
    public AdventureEvent Draw(AdventureState state)
    {
        // ---- Step 1: which entries are possible right now? ----

        List<Entry> possible = new List<Entry>();
        int totalWeight = 0;

        foreach (Entry entry in entries)
        {
            if (entry == null || entry.evt == null) continue;     // empty slot
            if (entry.weight <= 0) continue;                      // switched off
            if (entry.onlyOnce && state.HasSeen(entry.evt)) continue;
            if (!Requirement.AllMet(entry.requirements, state)) continue;

            possible.Add(entry);
            totalWeight += entry.weight;
        }

        if (possible.Count == 0) return null;

        // ---- Step 2: pick one, respecting the weights ----
        //
        // Imagine laying the entries end to end on a ruler, each taking up as
        // much space as its weight. Roll a random spot on the ruler, then walk
        // along subtracting weights until you land inside an entry.

        // Spelled out in full on purpose: plain "Random" breaks the moment a file
        // has both "using System;" and "using UnityEngine;", which is a classic
        // Unity head-scratcher.
        int roll = UnityEngine.Random.Range(0, totalWeight); // 0 up to totalWeight - 1

        foreach (Entry entry in possible)
        {
            roll -= entry.weight;

            if (roll < 0) return entry.evt;
        }

        // Cannot actually get here, but a return keeps the compiler happy.
        return possible[possible.Count - 1].evt;
    }

    // How many events could fire right now. Useful for "have I run dry?" checks
    // without actually drawing one.
    public int CountPossible(AdventureState state)
    {
        int count = 0;

        foreach (Entry entry in entries)
        {
            if (entry == null || entry.evt == null) continue;
            if (entry.weight <= 0) continue;
            if (entry.onlyOnce && state.HasSeen(entry.evt)) continue;
            if (!Requirement.AllMet(entry.requirements, state)) continue;

            count++;
        }

        return count;
    }

    // Every event in this pool, whether it is currently possible or not.
    // Drag these into your Adventure Book's "alsoInclude" list so that saving
    // and loading can find them.
    public List<AdventureEvent> GetAllEvents()
    {
        List<AdventureEvent> found = new List<AdventureEvent>();

        foreach (Entry entry in entries)
        {
            if (entry != null && entry.evt != null && !found.Contains(entry.evt))
            {
                found.Add(entry.evt);
            }
        }

        return found;
    }
}
