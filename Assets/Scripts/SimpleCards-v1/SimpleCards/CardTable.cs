using System.Collections.Generic;
using UnityEngine;

// THE WHOLE CARD GAME IN ONE OBJECT: the four piles, the energy, drawing,
// and the rules about whether a card can be played.
//
// It is a plain class, not a MonoBehaviour, so any script can own one and it is
// easy to test. It does the RULES; your UI does the DRAWING.
//
// The one thing it deliberately does NOT do is apply a card's effect. It hands
// you back which card was played and what it hit; dealing the damage is your
// game's business. That is why the same kit works for a deckbuilder, a card
// battler and a solitaire game.

public class CardTable
{
    public readonly CardPile drawPile = new CardPile("Draw");
    public readonly CardPile hand = new CardPile("Hand");
    public readonly CardPile discardPile = new CardPile("Discard");
    public readonly CardPile exilePile = new CardPile("Exile");

    // Cards past this many are simply not drawn. 0 means no limit.
    public int maxHandSize = 10;

    // Set false if your game has no energy or mana at all - costs are then
    // ignored completely.
    public bool useEnergy = true;

    public int energy = 3;
    public int maxEnergy = 3;

    // What can be pointed at RIGHT NOW. Your game keeps this up to date -
    // usually rebuilding it whenever something dies or spawns. The kit checks
    // chosen targets against it, so a card can never hit a dead enemy.
    public List<CardTarget> availableTargets = new List<CardTarget>();

    // ---------------- setting up ----------------

    // Builds a fresh deck from a list of card assets and shuffles it.
    // The list is allowed to contain the same card several times - that is how
    // you get three Strikes.
    public void StartNewGame(List<Card> deckList)
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exilePile.Clear();

        if (deckList != null)
        {
            foreach (Card card in deckList)
            {
                if (card == null) continue; // empty slot in the Inspector

                drawPile.AddToBottom(new CardInstance(card));
            }
        }

        drawPile.Shuffle();
        RefillEnergy();
    }

    public void RefillEnergy()
    {
        energy = maxEnergy;
    }

    // ---------------- drawing ----------------

    // Draws up to 'count' cards, reshuffling the discard pile back in when the
    // draw pile runs dry. Returns how many were ACTUALLY drawn, which can be
    // fewer if your hand filled up or you genuinely ran out of cards.
    public int Draw(int count)
    {
        int drawn = 0;

        for (int i = 0; i < count; i++)
        {
            if (maxHandSize > 0 && hand.Count >= maxHandSize) break;

            if (drawPile.IsEmpty)
            {
                ReshuffleDiscardIntoDraw();
            }

            // Still empty means every card is in your hand or exiled - there is
            // genuinely nothing left to draw.
            if (drawPile.IsEmpty) break;

            hand.AddToBottom(drawPile.TakeTop());
            drawn++;
        }

        return drawn;
    }

    // Tips the discard pile back into the draw pile and shuffles it.
    // Returns how many cards moved.
    public int ReshuffleDiscardIntoDraw()
    {
        int moved = discardPile.Count;

        discardPile.MoveAllTo(drawPile);
        drawPile.Shuffle();

        return moved;
    }

    // ---------------- playing ----------------

    // A quick check for greying out cards in the UI. It does NOT check targets,
    // because at draw time the player has not picked any yet.
    public bool CanAfford(CardInstance card)
    {
        if (card == null) return false;
        if (!useEnergy) return true;

        return energy >= card.GetCost();
    }

    // Plays a card. Returns what happened, including a plain-English reason if
    // it was refused. Nothing changes at all on a refusal.
    //
    // Pass the targets the player picked. For a card that needs none, pass null.
    // For an All card, pass null too - the kit works the targets out itself.
    public PlayResult Play(CardInstance card, List<CardTarget> chosenTargets)
    {
        if (card == null)
        {
            return PlayResult.Refused(null, "There is no card to play.");
        }

        if (card.card == null)
        {
            return PlayResult.Refused(card, "That card is empty.");
        }

        if (!hand.Contains(card))
        {
            return PlayResult.Refused(card, "That card is not in your hand.");
        }

        int cost = card.GetCost();

        if (useEnergy && energy < cost)
        {
            return PlayResult.Refused(card,
                "Not enough energy (needs " + cost + ", you have " + energy + ").");
        }

        // Work out and check the targets before changing anything.
        List<CardTarget> resolved = new List<CardTarget>();
        string problem = ResolveTargets(card.card, chosenTargets, resolved);

        if (problem != "")
        {
            return PlayResult.Refused(card, problem);
        }

        // Everything checks out, so now actually do it.
        if (useEnergy) energy -= cost;

        // The card leaves your hand BEFORE your effects run. That matters: a
        // card that says "draw two cards" must not be able to draw itself.
        hand.Take(card);
        SendToRestingPlace(card);

        return PlayResult.Success(card, resolved, useEnergy ? cost : 0);
    }

    // Works out which targets a card actually hits, or returns a sentence
    // explaining why it cannot be played. An empty string means "all fine".
    private string ResolveTargets(Card card, List<CardTarget> chosen, List<CardTarget> into)
    {
        if (card.targeting == Card.Targeting.None)
        {
            if (chosen != null && chosen.Count > 0)
            {
                return "That card does not take a target.";
            }
            return "";
        }

        if (card.targeting == Card.Targeting.All)
        {
            foreach (CardTarget target in availableTargets)
            {
                if (target != null && target.kind == card.targetKind)
                {
                    into.Add(target);
                }
            }

            if (into.Count == 0)
            {
                return "There is no " + card.targetKind + " to hit.";
            }

            return "";
        }

        // ChooseOne and ChooseMany.
        int needed = card.HowManyToChoose();
        int picked = (chosen != null) ? chosen.Count : 0;

        if (picked != needed)
        {
            return "That card needs " + needed + " " + card.targetKind
                   + " target" + ((needed == 1) ? "" : "s")
                   + ", but " + picked + " " + ((picked == 1) ? "was" : "were") + " picked.";
        }

        foreach (CardTarget target in chosen)
        {
            if (target == null)
            {
                return "One of the picked targets is empty.";
            }

            if (target.kind != card.targetKind)
            {
                return "You cannot play that on a " + target.kind + ".";
            }

            if (!availableTargets.Contains(target))
            {
                return target.GetName() + " is not a valid target any more.";
            }

            if (into.Contains(target))
            {
                return "You cannot pick " + target.GetName() + " twice.";
            }

            into.Add(target);
        }

        return "";
    }

    private void SendToRestingPlace(CardInstance card)
    {
        if (card.card.afterPlay == Card.AfterPlay.Exile)
        {
            exilePile.AddToBottom(card);
        }
        else if (card.card.afterPlay == Card.AfterPlay.BackToHand)
        {
            hand.AddToBottom(card);
        }
        else if (card.card.afterPlay == Card.AfterPlay.ToDrawPile)
        {
            drawPile.AddToBottom(card);
            drawPile.Shuffle();
        }
        else
        {
            discardPile.AddToBottom(card);
        }
    }

    // ---------------- tidying up ----------------

    // Puts one card from your hand into the discard pile without playing it.
    // Returns false if it was not in your hand.
    public bool Discard(CardInstance card)
    {
        if (!hand.Take(card)) return false;

        discardPile.AddToBottom(card);
        return true;
    }

    // The usual end of a turn.
    public int DiscardHand()
    {
        int discarded = hand.Count;

        hand.MoveAllTo(discardPile);

        return discarded;
    }

    // ---------------- looking at things ----------------

    // Every card anywhere - hand, draw, discard and exile. This is "your deck"
    // for a deck-viewing screen, and what you save between fights.
    public List<CardInstance> GetEveryCard()
    {
        List<CardInstance> all = new List<CardInstance>();

        all.AddRange(drawPile.GetAll());
        all.AddRange(hand.GetAll());
        all.AddRange(discardPile.GetAll());
        all.AddRange(exilePile.GetAll());

        return all;
    }

    // Just the targets of one kind, for building a "pick an enemy" UI.
    public List<CardTarget> GetTargetsOfKind(string kind)
    {
        List<CardTarget> found = new List<CardTarget>();

        foreach (CardTarget target in availableTargets)
        {
            if (target != null && target.kind == kind) found.Add(target);
        }

        return found;
    }

    // A snapshot of everything, for Debug.Log.
    public string Describe()
    {
        string energyLine = useEnergy ? ("Energy " + energy + "/" + maxEnergy) : "No energy in use";

        return energyLine
               + "\n  " + hand.Describe()
               + "\n  " + drawPile.Describe()
               + "\n  " + discardPile.Describe()
               + "\n  " + exilePile.Describe();
    }
}
