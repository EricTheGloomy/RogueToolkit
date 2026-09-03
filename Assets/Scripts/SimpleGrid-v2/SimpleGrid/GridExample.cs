using System.Collections.Generic;
using UnityEngine;

// A throwaway demo. Put it on any GameObject and press Play, then read the
// Console. It builds a 5x5 bag and four kinds of piece in code, so you see the
// whole system work with zero setup.
//
// Delete this file once it clicks. Nothing else needs it.

public class GridExample : MonoBehaviour
{
    void Start()
    {
        // ---- the bag ----------------------------------------------------
        // Drawn as text, same trick as the pieces. '#' is a square that cannot
        // be used - the awkward corners of a rucksack.
        PieceGrid bag = PieceGrid.FromMask(
            "....#\n" +
            ".....\n" +
            ".....\n" +
            ".....\n" +
            "#....");

        Debug.Log("Empty bag (" + bag.Width + "x" + bag.Height + "), "
                  + bag.CountFreeSquares() + " usable squares:\n" + bag.ToText());

        // ---- the pieces -------------------------------------------------
        GridPiece sword = MakeSword();
        GridPiece shield = MakeShield();
        GridPiece gem = MakeGem();
        GridPiece rations = MakeRations();

        ShowRotations(sword);

        // ---- fill the bag ------------------------------------------------
        // Place(piece, rotation, x, y) - x across, y DOWN from the top-left.
        PlacedPiece placedSword = bag.Place(sword, 0, 0, 0);
        PlacedPiece placedShield = bag.Place(shield, 0, 1, 0);
        PlacedPiece placedGem = bag.Place(gem, 0, 1, 2);

        if (placedSword == null || placedShield == null || placedGem == null)
        {
            Debug.LogError("Something did not fit - the demo is wrong, not you.");
            return;
        }

        // Try to put a sword somewhere it cannot go, to show the check working.
        if (bag.Place(sword, 0, 4, 4) == null)
        {
            Debug.Log("Correctly refused to place a sword hanging off the bottom edge.");
        }

        // Let the bag find a spot on its own - your "pick up loot" button.
        PlacedPiece placedRations = bag.PlaceAnywhere(rations);
        Debug.Log(placedRations != null
            ? "Auto-placed the rations at " + placedRations.x + "," + placedRations.y
              + ", rotation " + placedRations.rotation
            : "No room for the rations.");

        Report(bag, "AFTER PACKING");

        // ---- rearrange ---------------------------------------------------
        // A move that cannot work is refused and changes nothing, so a failed
        // drag can never lose the player's item.
        if (bag.TryMove(placedSword, 0, 1, 2) == null)
        {
            Debug.Log("Refused to move the sword onto the gem - nothing changed.");
        }

        // A move that does work hands back a NEW placed piece. The old one is
        // off the board, so always use the returned value from here on.
        PlacedPiece movedSword = bag.TryMove(placedSword, 1, 1, 3);

        if (movedSword != null)
        {
            placedSword = movedSword;
            Debug.Log("Laid the sword flat lower down - it is no longer touching the shield.");
            Report(bag, "AFTER REARRANGING - same pieces, different numbers");
        }

        // ---- take things out ----------------------------------------------
        bag.Remove(placedGem);
        bag.Remove(placedRations);
        Report(bag, "AFTER DROPPING THE GEM AND RATIONS");
    }

    // Prints the board plus every stat and where it came from.
    void Report(PieceGrid grid, string heading)
    {
        GridReport report = GridEvaluator.Evaluate(grid);

        string output = "=== " + heading + " ===\n"
                        + grid.ToText() + "\n"
                        + grid.ToLegend() + "\n";

        output += "\nTOTALS:";
        foreach (string statName in report.GetStatNames())
        {
            output += "  " + statName + " " + GridEvaluator.WithSign(report.GetTotal(statName));
        }

        output += "\n\nPER PIECE:";
        foreach (PieceReport piece in report.pieces)
        {
            output += "\n  " + piece.placed.GetDisplayName() + ": " + piece.DescribeStats();

            foreach (string why in piece.explanations)
            {
                output += "\n      " + why;
            }
        }

        Debug.Log(output);
    }

    void ShowRotations(GridPiece piece)
    {
        PieceShape shape = piece.GetShape();

        string output = piece.GetDisplayName() + " has " + shape.CellCount
                        + " squares and " + shape.RotationCount + " rotation(s):";

        for (int r = 0; r < shape.RotationCount; r++)
        {
            output += "\n\nrotation " + r + ":\n" + shape.ToMask(r);
        }

        Debug.Log(output);
    }

    // ---- the pieces ----------------------------------------------------
    // In a real project these are assets you make in the Project window. Built
    // in code here purely so the demo needs no setup.

    GridPiece MakeSword()
    {
        // A long sword: one square wide, three tall.
        GridPiece piece = MakePiece("Sword",
            "X\n" +
            "X\n" +
            "X");

        piece.tags.Add("weapon");
        piece.baseStats.Add(new StatAmount("attack", 3));

        // "For each touching piece tagged armour, I get +1 attack."
        piece.rules.Add(Rule(AdjacencyRule.Scope.Touching, "armour", "attack", 1));

        return piece;
    }

    GridPiece MakeShield()
    {
        // A 2x2 block. Note this has only ONE rotation, because turning a
        // square gets you the same square - the kit works that out for you.
        GridPiece piece = MakePiece("Shield",
            "XX\n" +
            "XX");

        piece.tags.Add("armour");
        piece.baseStats.Add(new StatAmount("defence", 4));

        // "If nothing at all is touching me, I get -2 defence."
        // A shield rattling around loose in an empty bag is worth less.
        AdjacencyRule lonely = Rule(AdjacencyRule.Scope.Touching, "", "defence", -2);
        lonely.counting = AdjacencyRule.Counting.OnceIfNoneFound;
        piece.rules.Add(lonely);

        return piece;
    }

    GridPiece MakeGem()
    {
        // A single square that buffs everything near it - the classic aura.
        GridPiece piece = MakePiece("Ruby Gem", "X");

        piece.tags.Add("gem");

        // "Give EACH piece within 2 squares +2 attack."
        AdjacencyRule aura = Rule(AdjacencyRule.Scope.WithinDistance, "", "attack", 2);
        aura.distance = 2;
        aura.target = AdjacencyRule.Target.EachMatchingNeighbour;
        piece.rules.Add(aura);

        return piece;
    }

    GridPiece MakeRations()
    {
        // An S/Z shape, which has TWO rotations.
        GridPiece piece = MakePiece("Rations",
            ".XX\n" +
            "XX.");

        piece.tags.Add("food");
        piece.baseStats.Add(new StatAmount("health", 5));

        return piece;
    }

    GridPiece MakePiece(string pieceName, string shapeMask)
    {
        GridPiece piece = ScriptableObject.CreateInstance<GridPiece>();
        piece.name = pieceName;
        piece.displayName = pieceName;
        piece.shapeMask = shapeMask;
        piece.RefreshShape();
        return piece;
    }

    AdjacencyRule Rule(AdjacencyRule.Scope scope, string neighbourTag,
                       string statName, int amount)
    {
        AdjacencyRule rule = new AdjacencyRule();
        rule.scope = scope;
        rule.neighbourTag = neighbourTag;
        rule.statName = statName;
        rule.amount = amount;
        return rule;
    }
}
