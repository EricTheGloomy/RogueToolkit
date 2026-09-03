# Simple Grid Pieces for Unity

Version 2 — 31 August 2026. Things on a grid that know where they are, what's
next to them, and what's on their row and column — and interact accordingly.
Backpack inventories, base building, tile puzzles, card boards.

Pieces can be any shape and rotate, **or be plain 1×1 squares** — see below.
They can be placed by the player or fixed in place by the game.

**New in v2:** `PlaceLocked` for things the game puts down and the player must
not move. Everything from v1 is unchanged.

## Why this is less scary than it looks

It's three problems that don't touch each other:

1. **Shape and rotation** — pure maths on a set of squares. Knows nothing about
   your game. ~200 lines, and hand-checked against paper.
2. **The board** — does it fit, what's in this square. A 2D array.
3. **The neighbour rules** — pure data you author in the Inspector.

None of them is hard on its own. It only looks hard when they're tangled
together, so they aren't.

## The trick that removes the pain

**You draw shapes as text.** No custom Inspector to write, and you can see the
shape at a glance:

```
.X.
XXX
```

Anything that isn't a dot, space or tab is a filled square, so `X`, `#` and `O`
all work. Rows read top to bottom. Rotations are worked out for you, and
rotations that come out identical are dropped — so a 2×2 square reports **one**
rotation and rotating it in your UI correctly appears to do nothing.

Boards are drawn the same way, with `#` for a square that can't be used:

```
....#
.....
#....
```

## If everything is 1×1

Perfectly normal, and probably the commonest case: a city grid, a slot
inventory, a board of cards. Just use `"X"` as the shape mask.

Every piece then reports **one** rotation, so the whole rotation system quietly
gets out of your way — `PlaceAnywhere` stops trying alternatives, rotating in
your UI correctly does nothing, and you can ignore the `rotation` argument
entirely (pass 0). You keep the part that actually matters: pieces knowing their
location, their neighbours, their row and column, and how they interact.

## Things the game places

Terrain, obstacles, a pre-built town hall, a well in the middle of the map —
things that take part in every rule but that the player must not touch:

```csharp
PlacedPiece well = map.PlaceLocked(wellPiece, 0, 2, 2);   // the game's
PlacedPiece hut  = map.Place(hutPiece, 0, 1, 1);          // the player's

map.Remove(well);            // false — refused
map.TryMove(well, 0, 4, 4);  // null  — refused
map.Remove(hut);             // true

map.ClearUnlocked();         // "reset my layout" — terrain survives
map.RemoveEvenIfLocked(well);// your own code can still destroy it
```

Locked pieces are ordinary pieces to the rules engine — they give and receive
bonuses exactly like any other. In `ToText()` they draw as a **lowercase**
letter and the legend marks them `(fixed)`, so a glance tells you which is which.

## What's in here

| File | What it is |
|---|---|
| `PieceShape.cs` | Text → squares → rotations. Pure maths, no Unity. |
| `AdjacencyRule.cs` | One "look around me" rule, plus the StatAmount pair. |
| `GridPiece.cs` | One kind of piece: shape, tags, base stats, rules. |
| `PieceGrid.cs` | The board, and PlacedPiece — one copy sitting on it. |
| `GridEvaluator.cs` | Works out what everything is worth, and explains why. |
| `GridExample.cs` | Zero-setup demo. Press Play, read the Console. Delete later. |
| `HowToUse.html` | The full guide. Open in a browser — not a Unity file. |

## Install

1. Copy the six `.cs` files into your project, anywhere under `Assets`.
2. **Move `HowToUse.html` and `README.md` out of `Assets`.**
3. Add the **Grid Example** component to an empty GameObject and press Play.

You'll get a 5×5 bag drawn in the Console, a sword's rotations, four pieces
packed in, a refused move, and the full stat breakdown before and after.

## Coordinates

`x` goes right, `y` goes **down**. `(0,0)` is the top-left square. That matches
how you read a shape mask and how Unity's UI fills a grid, so there's no mental
flipping.

## The 60-second version

```csharp
// A board
PieceGrid bag = new PieceGrid(5, 5);
PieceGrid odd = PieceGrid.FromMask("....#\n.....\n#....");

// Placing
if (bag.CanPlace(sword, rotation, x, y)) { }        // colour the drag preview
PlacedPiece placed = bag.Place(sword, rotation, x, y);   // null if it didn't fit
PlacedPiece loot   = bag.PlaceAnywhere(potion);          // "pick up" button
PlacedPiece rock   = bag.PlaceLocked(boulder, 0, x, y);  // the game's, immovable
bag.Remove(placed);                                      // refuses locked ones
bag.ClearUnlocked();                                     // reset the player's layout

// Moving — hands back a NEW placed piece, or null and changes nothing
PlacedPiece moved = bag.TryMove(placed, newRotation, x, y);

// Rotating in a UI: rotation numbers wrap, so this is safe forever
rotation++;

// What is it all worth?
GridReport report = GridEvaluator.Evaluate(bag);
int attack = report.GetTotal("attack");

// For a tooltip
PieceReport mine = report.GetReportFor(placed);
foreach (string line in mine.explanations) { /* "+2 attack from 2 touching weapon" */ }

// Debugging
Debug.Log(bag.ToText() + "\n" + bag.ToLegend());
```

## Writing rules

Read a rule left to right like a sentence. Common ones:

| What you want | scope | tag | target | counting | amount |
|---|---|---|---|---|---|
| +1 attack per touching weapon | Touching | weapon | Me | PerNeighbour | 1 |
| +5 defence if next to any food | Touching | food | Me | OnceIfAnyFound | 5 |
| −2 if nothing is touching me | Touching | *(empty)* | Me | OnceIfNoneFound | −2 |
| Buff everything within 2 squares | WithinDistance 2 | *(empty)* | EachMatchingNeighbour | — | 2 |
| Curse my orthogonal neighbours | Touching | *(empty)* | EachMatchingNeighbour | — | −2 |
| +1 gold per gem on my row | SameRow | gem | Me | PerNeighbour | 1 |

An empty tag means "any piece at all". Negative amounts are debuffs. `counting`
is ignored when the target is EachMatchingNeighbour — they each get the amount
once.

Both directions work, which matters: "I like being near weapons" is a rule on
the sword, "I make my neighbours stronger" is a rule on the gem. Use whichever
reads more naturally for the piece you're designing.

## Making it yours

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Weapon Piece")]
public class WeaponPiece : GridPiece
{
    public int damage;
    public AudioClip swingSound;
}
```

Then cast when you need your own fields: `WeaponPiece w = (WeaponPiece)placed.piece;`

## Four things worth knowing

**Neighbours are counted as distinct pieces, not shared edges.** A long sword
lying along a 2×2 shield touches it in two squares, but it's still *one* shield.
Counting it twice is the classic bug in a system like this, and there's a test
named after it.

**`TryMove` gives you a new `PlacedPiece`** and the old one is off the board.
Always use the returned value. If the move doesn't fit it returns null and
changes nothing, so a failed drag can't lose the player's item.

**Re-evaluate from scratch after any change.** Don't try to keep totals updated
incrementally — that's where these systems rot. On a board a human can arrange,
`Evaluate` is nothing.

**Ctrl+D copies a piece's hidden ID.** Right-click a piece asset →
**Print Shape And Rotations** is the fastest way to check you drew what you
meant; **Assign New Id** fixes a duplicate.

## Notes

`ToText()` and `ToLegend()` render the board as ASCII for `Debug.Log`. This is
by far the quickest way to work out why a piece isn't going where you expect —
use them before you reach for the debugger.

A piece never counts itself, even though its own squares touch each other.

An empty or all-dots shape mask falls back to a single square rather than
producing a piece with no squares, which would "fit" everywhere and place
nothing — a far more confusing bug than a visibly wrong shape.

## Verified

141 automated tests pass. The rotation maths is checked against shapes drawn on
paper first — every rotation of the I, O, T, S and L tetrominoes compared to a
hand-written expected mask, so a regression can't hide behind the code agreeing
with itself. Also covered: mask parsing (ragged rows, Windows line endings,
alternative fill characters, empty masks), symmetry deduplication, rotation
wrapping forwards and backwards, bounds and blocked squares, overlap refusal,
move-and-restore, auto-placement with rotation, locked pieces (refusing removal
and movement, the escape hatch, selective clearing, and taking part in the rules
normally), and every rule scope, counting mode and target — including
distinct-neighbour counting, self-exclusion, tag filtering, auras, debuffs, and
empty Inspector slots. Compiles clean in both editor and player builds.
