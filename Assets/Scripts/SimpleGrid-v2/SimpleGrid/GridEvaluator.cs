using System.Collections.Generic;
using UnityEngine;

// WHAT ONE PIECE ENDED UP WORTH, and why.
//
// The "why" matters more than you would think: it is what lets a tooltip say
// "+4 attack: 2 base, +2 from 2 touching weapons" instead of just "4", and it
// is how you debug a rule that is not doing what you meant.

public class PieceReport
{
    public readonly PlacedPiece placed;

    // stat name -> total for this piece
    public readonly Dictionary<string, int> stats = new Dictionary<string, int>();

    // Human-readable lines like "+2 attack from 2 touching weapon".
    public readonly List<string> explanations = new List<string>();

    public PieceReport(PlacedPiece placed)
    {
        this.placed = placed;
    }

    public int GetStat(string statName)
    {
        if (string.IsNullOrEmpty(statName)) return 0;

        int found;
        if (stats.TryGetValue(statName, out found)) return found;

        return 0;
    }

    public void Add(string statName, int amount, string why)
    {
        if (string.IsNullOrEmpty(statName)) return;
        if (amount == 0) return; // do not clutter the tooltip with "+0"

        stats[statName] = GetStat(statName) + amount;

        if (!string.IsNullOrEmpty(why))
        {
            explanations.Add(GridEvaluator.WithSign(amount) + " " + statName + " " + why);
        }
    }

    public string DescribeStats()
    {
        if (stats.Count == 0) return "no stats";

        List<string> parts = new List<string>();

        foreach (string statName in stats.Keys)
        {
            parts.Add(statName + " " + GridEvaluator.WithSign(stats[statName]));
        }

        parts.Sort();
        return string.Join(", ", parts.ToArray());
    }
}


// WHAT THE WHOLE BOARD IS WORTH.
public class GridReport
{
    public readonly List<PieceReport> pieces = new List<PieceReport>();

    // stat name -> total across every piece
    public readonly Dictionary<string, int> totals = new Dictionary<string, int>();

    public int GetTotal(string statName)
    {
        if (string.IsNullOrEmpty(statName)) return 0;

        int found;
        if (totals.TryGetValue(statName, out found)) return found;

        return 0;
    }

    // Every stat that ended up with a value, sorted so your UI is stable.
    public List<string> GetStatNames()
    {
        List<string> names = new List<string>(totals.Keys);
        names.Sort();
        return names;
    }

    public PieceReport GetReportFor(PlacedPiece placed)
    {
        foreach (PieceReport report in pieces)
        {
            if (report.placed == placed) return report;
        }

        return null;
    }
}


// WORKS OUT WHAT EVERYTHING IS WORTH once the pieces are on the board.
//
// Call Evaluate again after any change - a place, a move, a removal. It walks
// every piece and every rule, which on the size of board a human can arrange is
// nothing at all. Do not try to keep totals updated incrementally; recalculating
// from scratch is what makes this reliable.

public static class GridEvaluator
{
    public static GridReport Evaluate(PieceGrid grid)
    {
        GridReport report = new GridReport();

        if (grid == null) return report;

        List<PlacedPiece> placedPieces = grid.GetAllPieces();

        // Step 1: give every piece a report, starting with what it is worth on
        // its own. Every piece needs a report before rules run, because an aura
        // rule writes into somebody else's.
        foreach (PlacedPiece placed in placedPieces)
        {
            PieceReport pieceReport = new PieceReport(placed);
            report.pieces.Add(pieceReport);

            if (placed.piece == null) continue;

            foreach (StatAmount stat in placed.piece.baseStats)
            {
                if (stat == null) continue;

                pieceReport.Add(stat.statName, stat.amount, "base");
            }
        }

        // Step 2: run everybody's rules.
        foreach (PlacedPiece placed in placedPieces)
        {
            if (placed.piece == null) continue;

            foreach (AdjacencyRule rule in placed.piece.rules)
            {
                if (rule == null) continue;

                ApplyRule(grid, report, placed, rule);
            }
        }

        // Step 3: add everything up.
        foreach (PieceReport pieceReport in report.pieces)
        {
            foreach (string statName in pieceReport.stats.Keys)
            {
                int running = 0;
                report.totals.TryGetValue(statName, out running);
                report.totals[statName] = running + pieceReport.stats[statName];
            }
        }

        return report;
    }

    private static void ApplyRule(PieceGrid grid, GridReport report,
                                  PlacedPiece owner, AdjacencyRule rule)
    {
        List<PlacedPiece> matches = FindMatchingNeighbours(grid, owner, rule);

        if (rule.target == AdjacencyRule.Target.Me)
        {
            int amount = rule.AmountForMe(matches.Count);
            if (amount == 0) return;

            PieceReport mine = report.GetReportFor(owner);
            if (mine == null) return;

            mine.Add(rule.statName, amount, DescribeReasonForMe(rule, matches.Count));
            return;
        }

        // Target.EachMatchingNeighbour - an aura. Each match gets the amount once.
        foreach (PlacedPiece neighbour in matches)
        {
            PieceReport theirs = report.GetReportFor(neighbour);
            if (theirs == null) continue;

            theirs.Add(rule.statName, rule.amount, "from " + owner.GetDisplayName() + " " + rule.DescribeScope());
        }
    }

    // Finds the DISTINCT pieces around this one that the rule cares about.
    //
    // Distinct matters: a long sword touching a shield along three squares is
    // still one shield, and counting it three times is the classic bug in a
    // system like this.
    private static List<PlacedPiece> FindMatchingNeighbours(PieceGrid grid,
                                                            PlacedPiece owner,
                                                            AdjacencyRule rule)
    {
        List<PlacedPiece> found = new List<PlacedPiece>();

        foreach (Vector2Int square in SquaresToCheck(grid, owner, rule))
        {
            PlacedPiece there = grid.GetPieceAt(square.x, square.y);

            if (there == null) continue;
            if (there == owner) continue;              // never count yourself
            if (found.Contains(there)) continue;       // already counted this one
            if (!rule.Matches(there.piece)) continue;  // wrong tag

            found.Add(there);
        }

        return found;
    }

    // Every board square this rule looks at. Squares may repeat and may be
    // outside the board; the caller copes with both.
    private static List<Vector2Int> SquaresToCheck(PieceGrid grid,
                                                   PlacedPiece owner,
                                                   AdjacencyRule rule)
    {
        List<Vector2Int> squares = new List<Vector2Int>();

        if (rule.scope == AdjacencyRule.Scope.SameRow)
        {
            foreach (int row in DistinctRows(owner))
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    squares.Add(new Vector2Int(x, row));
                }
            }
            return squares;
        }

        if (rule.scope == AdjacencyRule.Scope.SameColumn)
        {
            foreach (int column in DistinctColumns(owner))
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    squares.Add(new Vector2Int(column, y));
                }
            }
            return squares;
        }

        // The three "near me" scopes are all the same idea with a different
        // reach, so one bit of code does all three.
        int range = 1;
        bool includeDiagonals = true;

        if (rule.scope == AdjacencyRule.Scope.Touching)
        {
            includeDiagonals = false;
        }
        else if (rule.scope == AdjacencyRule.Scope.WithinDistance)
        {
            range = (rule.distance < 1) ? 1 : rule.distance;
        }

        foreach (Vector2Int cell in owner.cells)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                for (int dx = -range; dx <= range; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    // Touching means sharing an EDGE, so exactly one of dx/dy
                    // may be non-zero.
                    if (!includeDiagonals && dx != 0 && dy != 0) continue;

                    squares.Add(new Vector2Int(cell.x + dx, cell.y + dy));
                }
            }
        }

        return squares;
    }

    private static List<int> DistinctRows(PlacedPiece placed)
    {
        List<int> rows = new List<int>();

        foreach (Vector2Int cell in placed.cells)
        {
            if (!rows.Contains(cell.y)) rows.Add(cell.y);
        }

        return rows;
    }

    private static List<int> DistinctColumns(PlacedPiece placed)
    {
        List<int> columns = new List<int>();

        foreach (Vector2Int cell in placed.cells)
        {
            if (!columns.Contains(cell.x)) columns.Add(cell.x);
        }

        return columns;
    }

    private static string DescribeReasonForMe(AdjacencyRule rule, int matchCount)
    {
        string what = rule.DescribeWhat();
        string where = rule.DescribeScope();

        if (rule.counting == AdjacencyRule.Counting.OnceIfNoneFound)
        {
            return "from no " + what + " " + where;
        }

        if (rule.counting == AdjacencyRule.Counting.OnceIfAnyFound)
        {
            return "from being " + where + " a " + what;
        }

        return "from " + matchCount + " " + where + " " + what;
    }

    // "+3" / "-2". Used everywhere the kit writes a number for a human.
    public static string WithSign(int amount)
    {
        if (amount >= 0) return "+" + amount;

        return amount.ToString(); // the minus sign is already there
    }
}
