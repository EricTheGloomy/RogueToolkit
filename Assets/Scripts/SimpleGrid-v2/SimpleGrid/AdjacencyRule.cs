using System.Collections.Generic;
using UnityEngine;

// ONE "LOOK AROUND ME AND CHANGE SOMETHING" RULE.
//
// This is where the interesting design lives, and it is all authored in the
// Inspector with dropdowns - you write no code per piece.
//
// Read a rule left to right like a sentence:
//
//   "For each TOUCHING piece tagged WEAPON, give ME +1 ATTACK"
//     scope=Touching, neighbourTag=weapon, target=Me, stat=attack, amount=1
//
//   "If NOTHING is touching me, give ME -2 MORALE"
//     scope=Touching, neighbourTag="", counting=OnceIfNoneFound, amount=-2
//
//   "Give EACH TOUCHING piece +1 ATTACK"   (an aura - the classic buff gem)
//     scope=Touching, target=EachMatchingNeighbour, stat=attack, amount=1

[System.Serializable]
public class AdjacencyRule
{
    // Which pieces count as "around me".
    public enum Scope
    {
        Touching,             // shares an edge - up, down, left, right
        TouchingOrDiagonal,   // shares an edge OR a corner
        WithinDistance,       // an aura: anything up to 'distance' squares away
        SameRow,              // anything on a row this piece sits on
        SameColumn,           // anything on a column this piece sits on
    }

    // How many times the amount is applied.
    public enum Counting
    {
        PerNeighbour,     // once for each matching piece found
        OnceIfAnyFound,   // a flat bonus if at least one matches
        OnceIfNoneFound,  // a flat penalty (or bonus) if nothing matches
    }

    // Who gets the stat.
    public enum Target
    {
        Me,                      // this piece gains it - "I like being near weapons"
        EachMatchingNeighbour,   // they gain it - "I make my neighbours stronger"
    }

    [Tooltip("Which pieces count as being 'around me'.")]
    public Scope scope = Scope.Touching;

    [Tooltip("Only used by the WithinDistance scope. 1 behaves like " +
             "TouchingOrDiagonal. Diagonals count as one step.")]
    public int distance = 2;

    [Tooltip("Only count neighbours with this tag. LEAVE EMPTY to count any piece at all.")]
    public string neighbourTag = "";

    [Tooltip("Who receives the stat: this piece, or each matching neighbour.")]
    public Target target = Target.Me;

    [Tooltip("How many times to apply the amount.\n\n" +
             "Ignored when the target is EachMatchingNeighbour - they always get " +
             "the amount once each.")]
    public Counting counting = Counting.PerNeighbour;

    [Tooltip("The stat to change: \"attack\", \"defence\", \"gold\". " +
             "Pick a spelling and stick to it.")]
    public string statName = "";

    [Tooltip("How much. Use a NEGATIVE number for a debuff.")]
    public int amount = 0;

    // Does this neighbour count for this rule? (The "is it near me" part is
    // handled by the evaluator, which knows about the board.)
    public bool Matches(GridPiece neighbour)
    {
        if (neighbour == null) return false;

        // An empty tag means "any piece will do".
        if (string.IsNullOrEmpty(neighbourTag)) return true;

        return neighbour.HasTag(neighbourTag);
    }

    // Works out the total for this rule given how many neighbours matched.
    // Only used when the target is Me.
    public int AmountForMe(int matchCount)
    {
        if (counting == Counting.PerNeighbour) return amount * matchCount;

        if (counting == Counting.OnceIfAnyFound) return (matchCount > 0) ? amount : 0;

        // OnceIfNoneFound
        return (matchCount == 0) ? amount : 0;
    }

    // A readable line for a tooltip, and for the explanations in the report.
    public string DescribeScope()
    {
        if (scope == Scope.Touching) return "touching";
        if (scope == Scope.TouchingOrDiagonal) return "touching or diagonal";
        if (scope == Scope.WithinDistance) return "within " + distance;
        if (scope == Scope.SameRow) return "on the same row";
        return "in the same column";
    }

    public string DescribeWhat()
    {
        return string.IsNullOrEmpty(neighbourTag) ? "piece" : neighbourTag;
    }
}

// A plain "this stat, this much" pair. Used for a piece's own stats, before any
// neighbours are considered.
[System.Serializable]
public class StatAmount
{
    [Tooltip("The stat name: \"attack\", \"defence\", \"weight\".")]
    public string statName = "";

    [Tooltip("How much. Negative is fine.")]
    public int amount = 0;

    public StatAmount() { }

    public StatAmount(string statName, int amount)
    {
        this.statName = statName;
        this.amount = amount;
    }
}
