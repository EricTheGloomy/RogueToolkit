using System.Collections.Generic;
using UnityEngine;

// A STACK OF CARDS: the draw pile, your hand, the discard pile, the exile pile.
//
// All four are the same thing. That is the whole reason this kit is small -
// there is no special "hand" code, just piles and moving cards between them.
//
// Position 0 is the TOP of the pile, which is where you draw from.

public class CardPile
{
    // "Draw", "Hand", "Discard" - only used for logging, but it makes
    // debug output readable.
    public readonly string name;

    private readonly List<CardInstance> cards = new List<CardInstance>();

    public CardPile(string name)
    {
        this.name = name;
    }

    public int Count
    {
        get { return cards.Count; }
    }

    public bool IsEmpty
    {
        get { return cards.Count == 0; }
    }

    public CardInstance GetAt(int index)
    {
        if (index < 0 || index >= cards.Count) return null;

        return cards[index];
    }

    // A copy, so your UI can loop over it while the pile changes underneath.
    public List<CardInstance> GetAll()
    {
        return new List<CardInstance>(cards);
    }

    public bool Contains(CardInstance card)
    {
        return card != null && cards.Contains(card);
    }

    // ---------------- putting cards in ----------------

    // Goes on top, so it is the next one drawn.
    public void AddToTop(CardInstance card)
    {
        if (card == null) return;

        cards.Insert(0, card);
    }

    // Goes underneath everything. This is the normal way to add to a hand, so
    // newly drawn cards appear on the right rather than jumping to the front.
    public void AddToBottom(CardInstance card)
    {
        if (card == null) return;

        cards.Add(card);
    }

    // ---------------- taking cards out ----------------

    // Takes the top card, or null if the pile is empty.
    public CardInstance TakeTop()
    {
        if (cards.Count == 0) return null;

        CardInstance top = cards[0];
        cards.RemoveAt(0);
        return top;
    }

    // Takes one specific card out, wherever it is. Returns false if it was not
    // in this pile - which is how the table checks a card is really in your hand
    // before letting you play it.
    public bool Take(CardInstance card)
    {
        if (card == null) return false;

        return cards.Remove(card);
    }

    public void Clear()
    {
        cards.Clear();
    }

    // ---------------- moving and shuffling ----------------

    // Empties this pile into another one, keeping the order.
    public void MoveAllTo(CardPile other)
    {
        if (other == null || other == this) return;

        foreach (CardInstance card in cards)
        {
            other.AddToBottom(card);
        }

        cards.Clear();
    }

    // A proper shuffle (Fisher-Yates): walk from the back, swapping each card
    // with a random one at or before it. Every order comes out equally likely,
    // which the "sort by a random number" trick does not manage.
    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            // Spelled out in full because plain "Random" is ambiguous in any
            // file that has both "using System;" and "using UnityEngine;".
            int swapWith = UnityEngine.Random.Range(0, i + 1);

            CardInstance held = cards[i];
            cards[i] = cards[swapWith];
            cards[swapWith] = held;
        }
    }

    // "Hand (3): Strike, Strike, Defend" - for Debug.Log.
    public string Describe()
    {
        if (cards.Count == 0) return name + " (empty)";

        List<string> names = new List<string>();

        foreach (CardInstance card in cards)
        {
            names.Add(card.GetDisplayName());
        }

        return name + " (" + cards.Count + "): " + string.Join(", ", names.ToArray());
    }
}
