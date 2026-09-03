using UnityEngine;

// SOMETHING A CARD CAN BE POINTED AT.
//
// This is the piece that keeps the kit reusable: it never learns what an
// "enemy" is. You tell it "here is a thing, its kind is 'enemy', and here is
// the object behind it", and it hands that object straight back to you when the
// card is played. What happens next is your code's business.
//
// Your game keeps table.availableTargets up to date - usually rebuilding it
// whenever something dies or spawns:
//
//     table.availableTargets.Clear();
//     foreach (Enemy enemy in aliveEnemies)
//     {
//         table.availableTargets.Add(new CardTarget("enemy", enemy.name, enemy));
//     }
//
// Then when a card resolves:
//
//     foreach (CardTarget target in result.targets)
//     {
//         Enemy enemy = (Enemy)target.thing;
//         enemy.TakeDamage(damage);
//     }

public class CardTarget
{
    // Matched against the card's targetKind. Your words, your meaning:
    // "enemy", "ally", "minion", "card". The kit only compares the strings.
    public readonly string kind;

    // What to show in the UI.
    public readonly string displayName;

    // The object behind it - your Enemy script, a GameObject, anything.
    // Spelled out in full because plain "Object" is ambiguous in any file that
    // has both "using System;" and "using UnityEngine;".
    public readonly UnityEngine.Object thing;

    // Set instead of 'thing' when the target IS a card, for effects like
    // "discard a card to draw two". Use CardTarget.ForCard to make one.
    public readonly CardInstance card;

    public CardTarget(string kind, string displayName, UnityEngine.Object thing)
    {
        this.kind = kind;
        this.displayName = displayName;
        this.thing = thing;
    }

    // A target that is a card in your hand. Its kind is "card".
    public static CardTarget ForCard(CardInstance card)
    {
        return new CardTarget("card", card);
    }

    private CardTarget(string kind, CardInstance card)
    {
        this.kind = kind;
        this.card = card;
        this.displayName = (card != null) ? card.GetDisplayName() : "<empty card>";
    }

    public string GetName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        if (thing != null) return thing.name;

        return "target";
    }

    public override string ToString()
    {
        return GetName() + " (" + kind + ")";
    }
}
