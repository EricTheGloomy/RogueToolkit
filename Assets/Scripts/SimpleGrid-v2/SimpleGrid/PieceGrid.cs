using System.Collections.Generic;
using UnityEngine;

// ONE COPY OF A PIECE, sitting on the board at a position and a rotation.
//
// A bag with three swords in it has ONE sword asset and THREE of these.
//
// The squares it covers are worked out once when it is placed and then kept,
// because nothing about a placed piece ever changes. To move it, you remove it
// and place it again - PieceGrid.TryMove does exactly that for you.

public class PlacedPiece
{
    public readonly GridPiece piece;
    public readonly int rotation;
    public readonly int x;
    public readonly int y;

    // TRUE for things the GAME put here, not the player: the well in the middle
    // of the map, the rock, the pre-built town hall. The player cannot move or
    // remove these. Your own code still can, with RemoveEvenIfLocked.
    public readonly bool locked;

    // Board squares this piece covers. Already in board coordinates, not offsets.
    public readonly List<Vector2Int> cells;

    public PlacedPiece(GridPiece piece, int rotation, int x, int y)
        : this(piece, rotation, x, y, false)
    {
    }

    public PlacedPiece(GridPiece piece, int rotation, int x, int y, bool locked)
    {
        this.piece = piece;
        this.rotation = rotation;
        this.x = x;
        this.y = y;
        this.locked = locked;

        cells = new List<Vector2Int>();

        if (piece == null) return;

        foreach (Vector2Int offset in piece.GetShape().GetCells(rotation))
        {
            cells.Add(new Vector2Int(x + offset.x, y + offset.y));
        }
    }

    public string GetDisplayName()
    {
        return (piece != null) ? piece.GetDisplayName() : "<empty>";
    }
}


// THE BOARD: a bag, a backpack, a base plot, a chess-like field.
//
// It is a plain class, not a ScriptableObject, because it changes constantly
// while the player rearranges things.
//
// Coordinates: x goes right, y goes DOWN. (0,0) is the top-left square, which
// matches how you draw a shape mask and how Unity's UI fills a grid.

public class PieceGrid
{
    private readonly int width;
    private readonly int height;

    // What is in each square, or null. This is the whole data structure - no
    // clever indexing, just a grid of references.
    private readonly PlacedPiece[,] occupants;

    // Squares that can never be used: the notch in an oddly-shaped bag, a rock
    // on a base plot.
    private readonly bool[,] blocked;

    private readonly List<PlacedPiece> pieces = new List<PlacedPiece>();

    public PieceGrid(int width, int height)
    {
        if (width < 1) width = 1;
        if (height < 1) height = 1;

        this.width = width;
        this.height = height;

        occupants = new PlacedPiece[width, height];
        blocked = new bool[width, height];
    }

    // Builds a board from a text mask, the same way you draw a piece:
    //
    //     ....#
    //     .....
    //     .....
    //     #....
    //
    // A '#' is a blocked square, anything else is usable. Handy for bags that
    // are not plain rectangles.
    public static PieceGrid FromMask(string mask)
    {
        string cleaned = (mask == null) ? "" : mask.Replace("\r", "");
        string[] lines = cleaned.Split('\n');

        int w = 0;
        foreach (string line in lines)
        {
            if (line.Length > w) w = line.Length;
        }

        PieceGrid grid = new PieceGrid(w, lines.Length);

        for (int y = 0; y < lines.Length; y++)
        {
            for (int x = 0; x < lines[y].Length; x++)
            {
                if (lines[y][x] == '#') grid.SetBlocked(x, y, true);
            }
        }

        return grid;
    }

    public int Width { get { return width; } }
    public int Height { get { return height; } }

    // ---------------- asking about squares ----------------

    public bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public bool IsBlocked(int x, int y)
    {
        if (!IsInside(x, y)) return true; // outside counts as blocked

        return blocked[x, y];
    }

    public void SetBlocked(int x, int y, bool isBlocked)
    {
        if (!IsInside(x, y)) return;

        blocked[x, y] = isBlocked;
    }

    public PlacedPiece GetPieceAt(int x, int y)
    {
        if (!IsInside(x, y)) return null;

        return occupants[x, y];
    }

    public bool IsFree(int x, int y)
    {
        return IsInside(x, y) && !blocked[x, y] && occupants[x, y] == null;
    }

    public List<PlacedPiece> GetAllPieces()
    {
        return new List<PlacedPiece>(pieces); // a copy, so callers cannot corrupt ours
    }

    public int Count { get { return pieces.Count; } }

    public int CountFreeSquares()
    {
        int free = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsFree(x, y)) free++;
            }
        }

        return free;
    }

    // ---------------- placing ----------------

    // Would this piece fit here? Use it to colour the drag preview green or red.
    public bool CanPlace(GridPiece piece, int rotation, int x, int y)
    {
        return CanPlace(piece, rotation, x, y, null);
    }

    // Same, but pretends one piece is not there. That is what you want while
    // dragging a piece around: it should be allowed to overlap where it
    // currently sits.
    public bool CanPlace(GridPiece piece, int rotation, int x, int y, PlacedPiece ignore)
    {
        if (piece == null) return false;

        foreach (Vector2Int offset in piece.GetShape().GetCells(rotation))
        {
            int cellX = x + offset.x;
            int cellY = y + offset.y;

            if (!IsInside(cellX, cellY)) return false;
            if (blocked[cellX, cellY]) return false;

            PlacedPiece sittingThere = occupants[cellX, cellY];

            if (sittingThere != null && sittingThere != ignore) return false;
        }

        return true;
    }

    // Puts a piece down. Returns the placed copy, or null if it did not fit.
    public PlacedPiece Place(GridPiece piece, int rotation, int x, int y)
    {
        return Place(piece, rotation, x, y, false);
    }

    // Puts down something the GAME owns: terrain, a pre-built structure, an
    // obstacle. It behaves like any other piece for the neighbour rules, but
    // the player cannot move or remove it.
    //
    // Use this when you lay out a level, and plain Place for what the player
    // builds.
    public PlacedPiece PlaceLocked(GridPiece piece, int rotation, int x, int y)
    {
        return Place(piece, rotation, x, y, true);
    }

    public PlacedPiece Place(GridPiece piece, int rotation, int x, int y, bool locked)
    {
        if (!CanPlace(piece, rotation, x, y)) return null;

        PlacedPiece placed = new PlacedPiece(piece, rotation, x, y, locked);

        foreach (Vector2Int cell in placed.cells)
        {
            occupants[cell.x, cell.y] = placed;
        }

        pieces.Add(placed);
        return placed;
    }

    // Takes a piece off the board. REFUSES anything the game locked down, so a
    // stray bulldoze button cannot delete the terrain.
    public bool Remove(PlacedPiece placed)
    {
        if (placed == null) return false;
        if (placed.locked) return false;

        return RemoveEvenIfLocked(placed);
    }

    // The escape hatch for your own code: destroys a locked piece anyway.
    // For when the town hall really has burnt down.
    public bool RemoveEvenIfLocked(PlacedPiece placed)
    {
        if (placed == null) return false;
        if (!pieces.Remove(placed)) return false; // not ours

        foreach (Vector2Int cell in placed.cells)
        {
            if (IsInside(cell.x, cell.y) && occupants[cell.x, cell.y] == placed)
            {
                occupants[cell.x, cell.y] = null;
            }
        }

        return true;
    }

    // Moves a piece, possibly rotating it. Returns the NEW placed piece, or null
    // if it would not fit - in which case the original is left exactly where it
    // was, so a failed drag cannot lose the player's item.
    //
    // Use the returned value from now on; the old one is no longer on the board.
    public PlacedPiece TryMove(PlacedPiece placed, int rotation, int x, int y)
    {
        if (placed == null) return null;
        if (placed.locked) return null; // the game put this here, not the player
        if (!pieces.Contains(placed)) return null;

        // Check with the piece itself ignored, so it may overlap its own squares.
        if (!CanPlace(placed.piece, rotation, x, y, placed)) return null;

        RemoveEvenIfLocked(placed);

        PlacedPiece moved = Place(placed.piece, rotation, x, y, placed.locked);

        if (moved == null)
        {
            // Should be impossible given the check above, but never leave the
            // player's piece in limbo.
            Place(placed.piece, placed.rotation, placed.x, placed.y, placed.locked);
            return null;
        }

        return moved;
    }

    // Finds the first free spot, trying every rotation. Returns null if there is
    // nowhere it fits. This is your "pick up loot straight into the bag" button.
    //
    // Scans top-left to bottom-right, trying rotation 0 first at each square.
    public PlacedPiece PlaceAnywhere(GridPiece piece)
    {
        if (piece == null) return null;

        int rotationCount = piece.GetShape().RotationCount;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int rotation = 0; rotation < rotationCount; rotation++)
                {
                    if (CanPlace(piece, rotation, x, y))
                    {
                        return Place(piece, rotation, x, y);
                    }
                }
            }
        }

        return null; // no room
    }

    // Empties the board completely, locked pieces and all.
    public void Clear()
    {
        pieces.Clear();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                occupants[x, y] = null;
            }
        }
    }

    // Clears only what the player put down, leaving the game's own terrain and
    // fixed structures in place. This is your "reset my layout" button.
    // Returns how many were removed.
    public int ClearUnlocked()
    {
        int removed = 0;

        // Backwards, because we are removing as we go.
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i].locked) continue;

            RemoveEvenIfLocked(pieces[i]);
            removed++;
        }

        return removed;
    }

    // ---------------- seeing what is going on ----------------

    // Draws the board as text for Debug.Log. Blocked squares are '#', empty
    // ones '.', and each piece gets a letter. Far and away the fastest way to
    // work out why something is not going where you expect.
    public string ToText()
    {
        List<string> lines = new List<string>();

        for (int y = 0; y < height; y++)
        {
            string line = "";

            for (int x = 0; x < width; x++)
            {
                if (blocked[x, y])
                {
                    line += '#';
                }
                else if (occupants[x, y] == null)
                {
                    line += '.';
                }
                else
                {
                    // A, B, C... in the order the pieces were placed.
                    // Lowercase for anything the game locked down.
                    int index = pieces.IndexOf(occupants[x, y]);
                    char letter = (char)('A' + (index % 26));

                    if (occupants[x, y].locked) letter = char.ToLower(letter);

                    line += letter;
                }
            }

            lines.Add(line);
        }

        return string.Join("\n", lines.ToArray());
    }

    // The key for ToText: "A = Sword, B = Shield".
    public string ToLegend()
    {
        List<string> parts = new List<string>();

        for (int i = 0; i < pieces.Count; i++)
        {
            char letter = (char)('A' + (i % 26));

            if (pieces[i].locked)
            {
                parts.Add(char.ToLower(letter) + " = " + pieces[i].GetDisplayName() + " (fixed)");
            }
            else
            {
                parts.Add(letter + " = " + pieces[i].GetDisplayName());
            }
        }

        return string.Join(", ", parts.ToArray());
    }
}
