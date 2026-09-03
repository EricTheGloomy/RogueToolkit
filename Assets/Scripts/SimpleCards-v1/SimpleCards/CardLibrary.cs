using System.Collections.Generic;
using RogueToolkit.Core.Validation;
using UnityEngine;

// The asset your game points at. Two jobs, both dull but necessary:
//
//   1. The STARTING DECK - what a new run begins with.
//   2. The LOOKUP - a saved deck stores IDs, and something has to turn those
//      back into real card assets.
//
// Make one per character or per game mode: WarriorDeck, RogueDeck.

[CreateAssetMenu(menuName = "Cards/Library")]
public class CardLibrary : ScriptableObject
{
    [Tooltip("Every card that can appear in this game mode. Needed so a saved " +
             "deck can be loaded back, and handy as the pool for card rewards.")]
    public List<Card> allCards = new List<Card>();

    [Tooltip("The deck a new run starts with. PUT THE SAME CARD IN SEVERAL TIMES " +
             "for several copies - three Strikes means dragging Strike in three times.")]
    public List<Card> startingDeck = new List<Card>();

    // ---------------- starting a game ----------------

    // A table already set up with the starting deck, shuffled.
    public CardTable StartNewGame()
    {
        CardTable table = new CardTable();
        table.StartNewGame(BuildStartingDeck());
        return table;
    }

    // The starting deck as a plain list, with empty Inspector slots removed.
    public List<Card> BuildStartingDeck()
    {
        List<Card> deck = new List<Card>();

        foreach (Card card in startingDeck)
        {
            if (card == null) continue;

            deck.Add(card);
        }

        return deck;
    }

    // ---------------- lookup ----------------

    // Turns a saved ID back into a real card. Returns null if that card was
    // deleted since the save was written, which is not an error - LoadDeck
    // just skips it.
    public Card FindCardById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (Card card in allCards)
        {
            if (card != null && card.GetId() == id)
            {
                return card;
            }
        }

        return null;
    }

    // Handy for debug commands. Not used by saving, because names can change.
    public Card FindCardByName(string cardName)
    {
        foreach (Card card in allCards)
        {
            if (card != null && card.name == cardName)
            {
                return card;
            }
        }

        return null;
    }

    // ---------------- saving a deck ----------------
    //
    // A deck is just a list of card IDs, duplicates and all. Pass it through
    // JsonUtility inside your own save class, or join it with commas.

    public List<string> SaveDeck(List<CardInstance> cards)
    {
        List<string> ids = new List<string>();

        if (cards == null) return ids;

        foreach (CardInstance instance in cards)
        {
            if (instance == null || instance.card == null) continue;

            ids.Add(instance.card.GetId());
        }

        return ids;
    }

    // Turns saved IDs back into a deck list you can hand to
    // CardTable.StartNewGame. Cards that no longer exist are skipped.
    public List<Card> LoadDeck(List<string> ids)
    {
        List<Card> deck = new List<Card>();

        if (ids == null) return deck;

        foreach (string id in ids)
        {
            Card card = FindCardById(id);

            if (card != null) deck.Add(card);
        }

        return deck;
    }

    // ---------------- validation ----------------

    // Checks the library configuration without changing it.
    //
    // The important checks here are the ones that can make cards impossible
    // to save/load or play correctly.
    public ValidationResult Validate()
    {
        ValidationResult result = new ValidationResult();

        if (allCards == null)
        {
            result.AddError("All cards list is null.", this);
            return result;
        }

        if (startingDeck == null)
        {
            result.AddError("Starting deck list is null.", this);
        }

        // Empty entries in All Cards are configuration errors.
        for (int i = 0; i < allCards.Count; i++)
        {
            if (allCards[i] == null)
            {
                result.AddError("All cards contains a null entry at index " + i + ".", this);
            }
        }

        // A duplicated ID is fatal for ID-based saving/loading.
        for (int i = 0; i < allCards.Count; i++)
        {
            Card first = allCards[i];
            if (first == null) continue;

            for (int j = i + 1; j < allCards.Count; j++)
            {
                Card second = allCards[j];
                if (second == null) continue;

                if (first.GetId() == second.GetId())
                {
                    result.AddError(
                        "Two cards share the same hidden ID: '" + first.name +
                        "' and '" + second.name + "'. Fix this by right-clicking '" +
                        second.name + "' and choosing 'Assign New Id'.",
                        second);
                }
            }
        }

        if (startingDeck != null)
        {
            for (int i = 0; i < startingDeck.Count; i++)
            {
                Card card = startingDeck[i];

                if (card == null)
                {
                    result.AddError(
                        "Starting deck contains a null entry at index " + i + ".",
                        this);
                    continue;
                }

                if (!allCards.Contains(card))
                {
                    result.AddError(
                        "'" + card.name + "' is in the starting deck but not in All Cards.",
                        card);
                }
            }
        }

        // A card that needs a target but has no kind can never be played.
        foreach (Card card in allCards)
        {
            if (card == null) continue;

            if (card.NeedsATarget() && string.IsNullOrEmpty(card.targetKind))
            {
                result.AddError(
                    "'" + card.name + "' needs a target but its Target Kind is empty, " +
                    "so it can never be played.",
                    card);
            }

            if (card.tags == null)
            {
                result.AddError(
                    "'" + card.name + "' has a null Tags list.",
                    card);
            }
        }

        return result;
    }

    // Logs validation issues in the Unity Console.
    // Kept separate from Validate() so the validation method remains useful to
    // tests and other tools that want to inspect the result themselves.
    private void LogValidationResult(ValidationResult result)
    {
        foreach (ValidationIssue issue in result.Issues)
        {
            if (issue.severity == ValidationIssue.Severity.Error)
            {
                Debug.LogError(issue.message, issue.source as Object);
            }
            else
            {
                Debug.LogWarning(issue.message, issue.source as Object);
            }
        }
    }

    // Right-click this asset in the Project window and choose "Check For Problems".
    // Run it after adding or duplicating cards.
    [ContextMenu("Check For Problems")]
    public void CheckForProblems()
    {
        ValidationResult result = Validate();

        if (!result.HasErrors)
        {
            Debug.Log("[" + name + "] No problems. " + allCards.Count + " cards, "
                      + startingDeck.Count + " in the starting deck.", this);
        }

        LogValidationResult(result);
    }
}
