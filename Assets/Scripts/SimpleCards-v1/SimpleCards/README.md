# Simple Cards for Unity

Version 1 — 31 August 2026. Deck, hand, discard, exile. Draw, play, target,
reshuffle. Completely separate from the other four kits.

## The idea that keeps it reusable

**The kit never learns what an "enemy" is.** You tell it what's targetable right
now, it validates what the player picked, and hands it straight back to you.
Dealing the damage is your code's business — which is why the same kit works for
a deckbuilder, a card battler and a solitaire game.

Two more things do most of the work:

**All four piles are the same class.** There's no special "hand" code — just
piles and moving cards between them.

**A card asset is a kind of card; a CardInstance is one physical copy.** A deck
with three Strikes has ONE Strike asset and THREE instances. That's the bit
people skip, and without it you can't tell one Strike in your hand from another,
so you can't discard *that one* or animate them separately.

## What's in here

| File | What it is |
|---|---|
| `Card.cs` | One kind of card: cost, targeting, where it goes after playing. |
| `CardInstance.cs` | One physical copy. Has its own id and its own cost tweak. |
| `CardTarget.cs` | Something a card can be pointed at. Your object, your meaning. |
| `CardPile.cs` | A stack of cards: draw, add, shuffle, move. Used for all four piles. |
| `PlayResult.cs` | What happened, including a plain-English reason for any refusal. |
| `CardTable.cs` | The four piles, energy, drawing, and the rules about playing. |
| `CardLibrary.cs` | The asset your game points at: starting deck, lookup, deck saving. |
| `CardExample.cs` | Zero-setup demo. Press Play, read the Console. Delete later. |
| `HowToUse.html` | The full guide. Open in a browser — not a Unity file. |

## Install

1. Copy the eight `.cs` files into your project, anywhere under `Assets`.
2. **Move `HowToUse.html` and `README.md` out of `Assets`.**
3. Add the **Card Example** component to an empty GameObject and press Play.

You'll see a 10-card deck dealt, four kinds of refusal explained, cards played
at a goblin, a card exiling itself and drawing two, and the discard pile
reshuffling when the draw pile runs dry.

## The 60-second version

```csharp
// Set up
CardTable table = library.StartNewGame();      // or new CardTable() + StartNewGame(deckList)

// Your game keeps this current — rebuild it whenever something dies or spawns
table.availableTargets.Clear();
foreach (Enemy e in aliveEnemies)
    table.availableTargets.Add(new CardTarget("enemy", e.name, e));

// A turn
table.Draw(5);
table.RefillEnergy();

// Playing
PlayResult result = table.Play(cardInstance, chosenTargets);   // null targets if it needs none

if (!result.played)
{
    messageLabel.text = result.refusedBecause;   // "Not enough energy (needs 2, you have 1)."
    return;
}

// It worked — now do the actual effect
AttackCard attack = (AttackCard)result.card.card;
foreach (CardTarget t in result.targets)
    ((Enemy)t.thing).TakeDamage(attack.damage);

// End of turn
table.DiscardHand();

// Debugging
Debug.Log(table.Describe());
```

## Targeting

Set these on the card asset:

| Targeting | What the player picks | Pass to Play |
|---|---|---|
| `None` | nothing | `null` |
| `ChooseOne` | one thing of `targetKind` | a list of 1 |
| `ChooseMany` | exactly `targetsToChoose` things | a list of that many |
| `All` | nothing — it hits every match | `null` |

`targetKind` is your own word: `"enemy"`, `"ally"`, `"minion"`, `"card"`. The kit
only compares strings — what they mean is up to you.

Every refusal comes back as a sentence you can show the player:

```
That card is not in your hand.
That card needs 1 enemy target, but 0 were picked.
A Dead Goblin is not a valid target any more.
Not enough energy (needs 1, you have 0).
You cannot play that on a ally.
You cannot pick Goblin twice.
```

## Making it yours

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Attack Card")]
public class AttackCard : Card
{
    public int damage;
    public GameObject hitEffect;
}
```

Then cast in your handler: `AttackCard a = (AttackCard)result.card.card;`

Use `as` plus a null check if your deck mixes several card types.

## Four things worth knowing

**The card leaves your hand before your effects run.** That's deliberate — a
card that says "draw 2 cards" must not be able to draw itself. It's already in
the discard (or exile) by the time you get the `PlayResult`.

**Rebuild `availableTargets` whenever something dies.** The kit refuses a target
that isn't on the list, which is what stops a card being played on a corpse —
but only if you keep the list honest.

**Nothing changes on a refusal.** Energy isn't spent, the card doesn't move. So
calling `Play` speculatively is always safe.

**Ctrl+D copies a card's hidden ID.** Right-click your Library →
**Check For Problems** catches that, plus empty slots, starting-deck cards
missing from the pool, and cards that need a target but have no `targetKind` set
(which can never be played).

## Notes

`useEnergy = false` turns costs off entirely, for games that have no mana.

`maxHandSize` stops the draw rather than discarding the overdraw; `Draw` returns
how many were *actually* drawn, which can be fewer than you asked for.

Drawing reshuffles the discard pile back in automatically when the draw pile runs
dry. If both are empty, `Draw` returns fewer rather than looping.

`afterPlay` has four settings: `Discard`, `Exile`, `BackToHand`, `ToDrawPile`.

The shuffle is a proper Fisher-Yates, so every order is equally likely — the
"sort by a random number" trick isn't. Call `UnityEngine.Random.InitState(seed)`
before a shuffle to make a bug reproducible.

## Verified

102 automated tests pass, covering: instance identity and per-copy cost changes,
pile order for top and bottom insertion, shuffle fairness (nothing lost or
duplicated) and seed reproducibility, the reshuffle-when-empty loop, running out
of cards entirely, hand limits, all four after-play destinations, the card
leaving hand before effects, every refusal reason including wrong kind, duplicate
picks, corpses and insufficient energy, All-targeting with nothing to hit,
energy switched off, deck save/load preserving duplicates and surviving renames,
and every library problem check. Compiles clean in both editor and player builds.
