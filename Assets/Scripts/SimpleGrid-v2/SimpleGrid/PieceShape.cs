using System.Collections.Generic;
using UnityEngine;

// A PIECE'S SHAPE, and its rotations.
//
// You draw the shape as TEXT. That is the trick that makes this whole kit small:
// no custom Inspector to write, and you can see the shape at a glance.
//
//     .X.
//     XXX
//
// Anything that is not a dot, a space or a tab counts as a filled square, so
// X, #, O all work. Rows are read top to bottom, so y goes DOWN - the same
// direction you read, and the same direction Unity's UI fills a grid.
//
// This class is pure maths. It knows nothing about Unity, your game, or what
// the piece is for - which is why it is the easy part.

public class PieceShape
{
    // rotations[0] is the shape as you drew it, [1] is 90 degrees clockwise,
    // and so on. Rotations that come out looking identical are thrown away, so
    // a 2x2 square has ONE rotation and a straight line has TWO. That means
    // "rotate" in your UI never appears to do nothing.
    private List<List<Vector2Int>> rotations = new List<List<Vector2Int>>();

    public int RotationCount
    {
        get { return rotations.Count; }
    }

    // How many squares the piece covers.
    public int CellCount
    {
        get { return (rotations.Count > 0) ? rotations[0].Count : 0; }
    }

    // The squares this rotation covers, as offsets from its top-left corner.
    // Rotation numbers wrap around, so you can just do rotation++ forever
    // without ever checking the range.
    public List<Vector2Int> GetCells(int rotation)
    {
        if (rotations.Count == 0) return new List<Vector2Int>();

        return rotations[WrapRotation(rotation)];
    }

    // Turns any number into a valid rotation index. Handles negatives too, so
    // rotation-- works as well.
    public int WrapRotation(int rotation)
    {
        if (rotations.Count <= 0) return 0;

        int wrapped = rotation % rotations.Count;
        if (wrapped < 0) wrapped += rotations.Count;

        return wrapped;
    }

    public int GetWidth(int rotation)
    {
        return WidthOf(GetCells(rotation));
    }

    public int GetHeight(int rotation)
    {
        return HeightOf(GetCells(rotation));
    }

    // ---------------- building ----------------

    public static PieceShape FromMask(string mask)
    {
        List<Vector2Int> cells = ParseMask(mask);

        // A piece with no squares would "fit" everywhere and place nothing,
        // which is a much more confusing bug than a visible wrong shape.
        if (cells.Count == 0)
        {
            cells.Add(new Vector2Int(0, 0));
        }

        PieceShape shape = new PieceShape();
        shape.BuildRotations(cells);
        return shape;
    }

    // Reads a text mask into a list of squares. Also used by PieceGrid to read
    // board shapes, so the same drawing trick works for both.
    public static List<Vector2Int> ParseMask(string mask)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        if (string.IsNullOrEmpty(mask)) return cells;

        // Windows line endings would leave a stray \r on every row.
        string cleaned = mask.Replace("\r", "");
        string[] lines = cleaned.Split('\n');

        for (int y = 0; y < lines.Length; y++)
        {
            string line = lines[y];

            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];

                if (c == '.' || c == ' ' || c == '\t') continue;

                cells.Add(new Vector2Int(x, y));
            }
        }

        Normalise(cells);
        return cells;
    }

    // Draws a rotation back out as text. Handy in Debug.Log when a piece is not
    // going where you expect.
    public string ToMask(int rotation)
    {
        List<Vector2Int> cells = GetCells(rotation);

        if (cells.Count == 0) return "";

        int width = WidthOf(cells);
        int height = HeightOf(cells);

        // Start with a grid of dots, then fill in the squares.
        char[,] picture = new char[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                picture[x, y] = '.';
            }
        }

        foreach (Vector2Int cell in cells)
        {
            picture[cell.x, cell.y] = 'X';
        }

        List<string> lines = new List<string>();

        for (int y = 0; y < height; y++)
        {
            string line = "";

            for (int x = 0; x < width; x++)
            {
                line += picture[x, y];
            }

            lines.Add(line);
        }

        return string.Join("\n", lines.ToArray());
    }

    // ---------------- the rotation maths ----------------

    private void BuildRotations(List<Vector2Int> cells)
    {
        List<Vector2Int> current = cells;

        // Four quarter-turns brings us back to the start, so four is all we
        // ever need to try.
        for (int i = 0; i < 4; i++)
        {
            if (!AlreadyHave(current))
            {
                rotations.Add(current);
            }

            current = RotateClockwise(current);
        }
    }

    // Turning the page a quarter turn clockwise: a square that was near the TOP
    // ends up near the RIGHT. So the old y becomes the new x, counting
    // backwards, and the old x becomes the new y.
    private static List<Vector2Int> RotateClockwise(List<Vector2Int> cells)
    {
        int height = HeightOf(cells);

        List<Vector2Int> turned = new List<Vector2Int>();

        foreach (Vector2Int cell in cells)
        {
            turned.Add(new Vector2Int((height - 1) - cell.y, cell.x));
        }

        Normalise(turned);
        return turned;
    }

    // Slides the squares so the shape's top-left corner sits at (0,0). Without
    // this, rotating would drift the piece across the board.
    private static void Normalise(List<Vector2Int> cells)
    {
        if (cells.Count == 0) return;

        int minX = cells[0].x;
        int minY = cells[0].y;

        foreach (Vector2Int cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }

        if (minX == 0 && minY == 0) return;

        for (int i = 0; i < cells.Count; i++)
        {
            cells[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);
        }
    }

    private bool AlreadyHave(List<Vector2Int> cells)
    {
        string key = KeyFor(cells);

        foreach (List<Vector2Int> existing in rotations)
        {
            if (KeyFor(existing) == key) return true;
        }

        return false;
    }

    // Turns a set of squares into one comparable string, so we can tell whether
    // two rotations look the same. Sorted, because the order of the list does
    // not matter - only which squares are covered.
    private static string KeyFor(List<Vector2Int> cells)
    {
        List<string> parts = new List<string>();

        foreach (Vector2Int cell in cells)
        {
            parts.Add(cell.x + "," + cell.y);
        }

        parts.Sort();

        return string.Join(";", parts.ToArray());
    }

    private static int WidthOf(List<Vector2Int> cells)
    {
        int max = -1;

        foreach (Vector2Int cell in cells)
        {
            if (cell.x > max) max = cell.x;
        }

        return max + 1;
    }

    private static int HeightOf(List<Vector2Int> cells)
    {
        int max = -1;

        foreach (Vector2Int cell in cells)
        {
            if (cell.y > max) max = cell.y;
        }

        return max + 1;
    }
}
