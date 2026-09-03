using System.Collections.Generic;
using UnityEngine;

// A THROWAWAY VIEW for the SimpleGrid kit, so you can actually FEEL whether
// packing things is satisfying. You cannot learn that from Debug.Log.
//
// HOW TO USE: add this to any empty GameObject and press Play. That is all -
// no Canvas, no prefabs, no scene setup, no art.
//
//   click a palette button (or press 1-6)  pick what to place
//   move the mouse over the board          see a green/red ghost
//   left click                             place it
//   right click a piece                    remove it
//   R                                      rotate
//   C                                      clear your layout (terrain stays)
//
//
// WHY IS THIS OnGUI AND NOT PROPER UNITY UI?
//
// On purpose. OnGUI is Unity's old immediate-mode UI. It is the wrong tool for
// a real game and the right tool for this, because:
//
//   - it needs no Canvas, no prefabs, no fonts, no packages - it just draws
//   - it cannot be mistaken for your real UI, so you will not be tempted to
//     build on it and then be unable to update the kit underneath
//   - it is short enough to read in one sitting
//
// When you build the real thing with uGUI or UI Toolkit, note what changes in
// the kit itself: NOTHING. That is the whole point being demonstrated here.
// This file talks to PieceGrid and GridEvaluator exactly the way your real UI
// will, it just paints rectangles instead of sprites.
//
// Delete this file when you are done with it.

public class GridPrototypeView : MonoBehaviour
{
    [Header("Board")]
    [Tooltip("Leave the mask empty for a plain rectangle of this size.")]
    public int boardWidth = 6;
    public int boardHeight = 6;

    [TextArea(3, 8)]
    [Tooltip("Optional. Draw the board here instead - '#' is an unusable square.")]
    public string boardMask = "";

    [Header("Pieces")]
    [Tooltip("Leave empty to use the built-in demo pieces.")]
    public List<GridPiece> palette = new List<GridPiece>();

    [Tooltip("Optional. Placed by the game at the start, and the player cannot move them.")]
    public List<GridPiece> fixedPieces = new List<GridPiece>();

    [Header("Look")]
    public int cellSize = 46;
    public int boardLeft = 24;
    public int boardTop = 64;

    // ---- state ----
    private PieceGrid grid;
    private GridReport report;
    private int selected = 0;
    private int rotation = 0;
    private string message = "";

    // A single white pixel, tinted to draw every coloured rectangle.
    private Texture2D dot;

    private static readonly Color[] pieceColours =
    {
        new Color(0.28f, 0.52f, 0.82f),
        new Color(0.84f, 0.45f, 0.33f),
        new Color(0.38f, 0.68f, 0.44f),
        new Color(0.76f, 0.60f, 0.24f),
        new Color(0.58f, 0.44f, 0.74f),
        new Color(0.32f, 0.68f, 0.68f),
    };

    private static readonly Color lockedColour = new Color(0.42f, 0.43f, 0.47f);
    private static readonly Color emptyColour = new Color(0.19f, 0.21f, 0.24f);
    private static readonly Color blockedColour = new Color(0.09f, 0.09f, 0.11f);
    private static readonly Color okGhost = new Color(0.35f, 0.85f, 0.45f, 0.55f);
    private static readonly Color badGhost = new Color(0.90f, 0.35f, 0.30f, 0.55f);

    void Start()
    {
        if (palette.Count == 0) BuildDemoPieces();

        grid = string.IsNullOrEmpty(boardMask)
            ? new PieceGrid(boardWidth, boardHeight)
            : PieceGrid.FromMask(boardMask);

        // Whatever the game owns goes down first, in the middle-ish, locked.
        foreach (GridPiece piece in fixedPieces)
        {
            if (piece == null) continue;

            PlacedPiece placed = PlaceLockedAnywhere(piece);
            if (placed == null) Debug.LogWarning("No room for the fixed piece " + piece.name);
        }

        Recalculate();
    }

    void Update()
    {
        // Keyboard in Update, mouse in OnGUI - that way the y axis is
        // consistently "down from the top" wherever we deal with positions.
        if (Input.GetKeyDown(KeyCode.R)) rotation++;
        if (Input.GetKeyDown(KeyCode.C)) { grid.ClearUnlocked(); Recalculate(); message = "Cleared your layout."; }

        for (int i = 0; i < palette.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { selected = i; rotation = 0; }
        }
    }

    void OnGUI()
    {
        EnsureDot();

        GUI.Label(new Rect(boardLeft, 12, 800, 22),
                  "SimpleGrid prototype view - click to place, right click to remove, R rotate, C clear");

        DrawBoard();
        DrawGhost();
        DrawPalette();
        DrawTotals();

        HandleMouse();

        if (message != "")
        {
            GUI.Label(new Rect(boardLeft, boardTop + boardPixelHeight() + 10, 700, 22), message);
        }
    }

    // ---------------- drawing ----------------

    void DrawBoard()
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Rect cell = CellRect(x, y);

                if (grid.IsBlocked(x, y))
                {
                    Box(cell, blockedColour);
                    continue;
                }

                PlacedPiece here = grid.GetPieceAt(x, y);

                if (here == null)
                {
                    Box(cell, emptyColour);
                    continue;
                }

                Box(cell, here.locked ? lockedColour : ColourFor(here.piece));

                // Only write the name on the piece's top-left square, so a long
                // piece is not covered in repeated text.
                if (here.x == x && here.y == y)
                {
                    GUI.Label(new Rect(cell.x + 4, cell.y + 2, cellSize * 3, 18),
                              here.GetDisplayName());
                }
            }
        }
    }

    void DrawGhost()
    {
        GridPiece piece = SelectedPiece();
        if (piece == null) return;

        int x, y;
        if (!MouseCell(out x, out y)) return;

        bool fits = grid.CanPlace(piece, rotation, x, y);

        foreach (Vector2Int offset in piece.GetShape().GetCells(rotation))
        {
            Box(CellRect(x + offset.x, y + offset.y), fits ? okGhost : badGhost);
        }
    }

    void DrawPalette()
    {
        int left = boardLeft + boardPixelWidth() + 24;
        int top = boardTop;

        GUI.Label(new Rect(left, top - 22, 300, 22), "PLACE  (or press 1-" + palette.Count + ")");

        for (int i = 0; i < palette.Count; i++)
        {
            GridPiece piece = palette[i];
            if (piece == null) continue;

            Rect swatch = new Rect(left, top + i * 30, 18, 18);
            Box(swatch, ColourFor(piece));

            string label = (i + 1) + "  " + piece.GetDisplayName()
                           + "  (" + piece.GetShape().CellCount + " sq)";

            if (i == selected) label = "> " + label;

            if (GUI.Button(new Rect(left + 24, top + i * 30 - 2, 220, 24), label))
            {
                selected = i;
                rotation = 0;
            }
        }

        GridPiece current = SelectedPiece();
        if (current != null)
        {
            int shapeTop = top + palette.Count * 30 + 16;
            GUI.Label(new Rect(left, shapeTop, 300, 22),
                      "rotation " + current.GetShape().WrapRotation(rotation)
                      + " of " + current.GetShape().RotationCount + "   (R to turn)");

            // Draw the selected shape small, so you can see what R is doing.
            foreach (Vector2Int offset in current.GetShape().GetCells(rotation))
            {
                Box(new Rect(left + offset.x * 16, shapeTop + 24 + offset.y * 16, 15, 15),
                    ColourFor(current));
            }
        }
    }

    void DrawTotals()
    {
        int left = boardLeft + boardPixelWidth() + 24;
        int top = boardTop + palette.Count * 30 + 140;

        GUI.Label(new Rect(left, top, 300, 22), "TOTALS");

        int line = 1;
        foreach (string stat in report.GetStatNames())
        {
            GUI.Label(new Rect(left, top + line * 20, 300, 22),
                      stat + "  " + GridEvaluator.WithSign(report.GetTotal(stat)));
            line++;
        }

        if (line == 1) GUI.Label(new Rect(left, top + 20, 300, 22), "(nothing placed yet)");

        // What is under the cursor, and why it is worth what it is worth.
        int cx, cy;
        if (!MouseCell(out cx, out cy)) return;

        PlacedPiece hovered = grid.GetPieceAt(cx, cy);
        if (hovered == null) return;

        PieceReport detail = report.GetReportFor(hovered);
        if (detail == null) return;

        int detailTop = top + line * 20 + 24;
        GUI.Label(new Rect(left, detailTop, 340, 22),
                  hovered.GetDisplayName() + (hovered.locked ? "  (fixed)" : "")
                  + ":  " + detail.DescribeStats());

        for (int i = 0; i < detail.explanations.Count; i++)
        {
            GUI.Label(new Rect(left + 12, detailTop + 20 + i * 18, 340, 20), detail.explanations[i]);
        }
    }

    // ---------------- input ----------------

    void HandleMouse()
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown) return;

        int x, y;
        if (!MouseCell(out x, out y)) return;

        if (e.button == 0)
        {
            GridPiece piece = SelectedPiece();
            if (piece == null) return;

            if (grid.Place(piece, rotation, x, y) != null)
            {
                message = "Placed " + piece.GetDisplayName() + " at " + x + "," + y;
                Recalculate();
            }
            else
            {
                message = "That does not fit there.";
            }

            e.Use();
        }
        else if (e.button == 1)
        {
            PlacedPiece there = grid.GetPieceAt(x, y);

            if (there == null)
            {
                message = "Nothing there to remove.";
            }
            else if (grid.Remove(there))
            {
                message = "Removed " + there.GetDisplayName();
                Recalculate();
            }
            else
            {
                // The one refusal worth showing off.
                message = there.GetDisplayName() + " was placed by the game - you cannot move it.";
            }

            e.Use();
        }
    }

    // Which board square is the mouse over? False if it is off the board.
    //
    // This is the only real maths in the whole view, and it is the same two
    // lines whatever UI system you end up using.
    bool MouseCell(out int x, out int y)
    {
        x = 0;
        y = 0;

        if (Event.current == null) return false;

        Vector2 mouse = Event.current.mousePosition;

        x = Mathf.FloorToInt((mouse.x - boardLeft) / cellSize);
        y = Mathf.FloorToInt((mouse.y - boardTop) / cellSize);

        return grid.IsInside(x, y);
    }

    // ---------------- little helpers ----------------

    void Recalculate()
    {
        // From scratch, every time. One piece moving can change three others'
        // bonuses through an aura, so incremental updates are a trap.
        report = GridEvaluator.Evaluate(grid);
    }

    GridPiece SelectedPiece()
    {
        if (selected < 0 || selected >= palette.Count) return null;
        return palette[selected];
    }

    Rect CellRect(int x, int y)
    {
        // The 1px gap is what makes it read as a grid rather than a blob.
        return new Rect(boardLeft + x * cellSize, boardTop + y * cellSize, cellSize - 1, cellSize - 1);
    }

    int boardPixelWidth() { return grid.Width * cellSize; }
    int boardPixelHeight() { return grid.Height * cellSize; }

    Color ColourFor(GridPiece piece)
    {
        int index = palette.IndexOf(piece);

        if (index < 0) return lockedColour; // not in the palette, so the game's

        return pieceColours[index % pieceColours.Length];
    }

    void Box(Rect area, Color colour)
    {
        Color was = GUI.color;
        GUI.color = colour;
        GUI.DrawTexture(area, dot);
        GUI.color = was;
    }

    void EnsureDot()
    {
        if (dot != null) return;

        dot = new Texture2D(1, 1);
        dot.SetPixel(0, 0, Color.white);
        dot.Apply();
    }

    PlacedPiece PlaceLockedAnywhere(GridPiece piece)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.CanPlace(piece, 0, x, y)) return grid.PlaceLocked(piece, 0, x, y);
            }
        }
        return null;
    }

    // ---------------- the demo pieces ----------------
    // Only used when you leave the palette empty. In a real project these are
    // assets you make in the Project window.

    void BuildDemoPieces()
    {
        GridPiece sword = MakePiece("Sword", "X\nX\nX", "weapon");
        sword.baseStats.Add(new StatAmount("attack", 3));
        sword.rules.Add(MakeRule(AdjacencyRule.Scope.Touching, "armour", "attack", 1));

        GridPiece shield = MakePiece("Shield", "XX\nXX", "armour");
        shield.baseStats.Add(new StatAmount("defence", 4));

        GridPiece boots = MakePiece("Boots", ".XX\nXX.", "gear");
        boots.baseStats.Add(new StatAmount("speed", 2));

        GridPiece gem = MakePiece("Gem", "X", "gem");
        AdjacencyRule aura = MakeRule(AdjacencyRule.Scope.WithinDistance, "", "attack", 2);
        aura.distance = 2;
        aura.target = AdjacencyRule.Target.EachMatchingNeighbour;
        gem.rules.Add(aura);

        palette.Add(sword);
        palette.Add(shield);
        palette.Add(boots);
        palette.Add(gem);

        if (fixedPieces.Count == 0)
        {
            GridPiece rock = MakePiece("Rock", "XX\nX.");
            fixedPieces.Add(rock);
        }
    }

    GridPiece MakePiece(string pieceName, string mask, params string[] tags)
    {
        GridPiece piece = ScriptableObject.CreateInstance<GridPiece>();
        piece.name = pieceName;
        piece.displayName = pieceName;
        piece.shapeMask = mask;
        piece.RefreshShape();
        foreach (string tag in tags) piece.tags.Add(tag);
        return piece;
    }

    AdjacencyRule MakeRule(AdjacencyRule.Scope scope, string tag, string stat, int amount)
    {
        AdjacencyRule rule = new AdjacencyRule();
        rule.scope = scope;
        rule.neighbourTag = tag;
        rule.statName = stat;
        rule.amount = amount;
        return rule;
    }
}
