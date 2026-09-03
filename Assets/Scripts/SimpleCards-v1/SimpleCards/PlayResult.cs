using System.Collections.Generic;

// WHAT HAPPENED WHEN YOU TRIED TO PLAY A CARD.
//
// Every refusal comes with a reason in plain English. That is deliberate: it is
// your "you can't do that" message, and it is how you work out why a card is not
// playable when you were sure it should be.
//
//     PlayResult result = table.Play(card, chosenTargets);
//
//     if (!result.played)
//     {
//         messageLabel.text = result.refusedBecause;   // "Not enough energy (needs 2, you have 1)."
//         return;
//     }
//
//     // It worked. Now do the actual effect.
//     foreach (CardTarget target in result.targets) { ... }

public class PlayResult
{
    // Did it actually get played?
    public readonly bool played;

    // Empty when it worked. A sentence you can show the player when it did not.
    public readonly string refusedBecause;

    public readonly CardInstance card;

    // The targets it ended up hitting. For an All card this is worked out for
    // you, so it may hold more than the player picked. Never null - an empty
    // list for a card that needs no target.
    public readonly List<CardTarget> targets;

    public readonly int energySpent;

    private PlayResult(bool played, string refusedBecause, CardInstance card,
                       List<CardTarget> targets, int energySpent)
    {
        this.played = played;
        this.refusedBecause = refusedBecause;
        this.card = card;
        this.targets = (targets != null) ? targets : new List<CardTarget>();
        this.energySpent = energySpent;
    }

    public static PlayResult Refused(CardInstance card, string because)
    {
        return new PlayResult(false, because, card, null, 0);
    }

    public static PlayResult Success(CardInstance card, List<CardTarget> targets, int energySpent)
    {
        return new PlayResult(true, "", card, targets, energySpent);
    }

    public override string ToString()
    {
        if (!played) return "refused: " + refusedBecause;

        string line = "played " + ((card != null) ? card.ToString() : "<none>");

        if (targets.Count > 0)
        {
            List<string> names = new List<string>();
            foreach (CardTarget target in targets) names.Add(target.GetName());
            line += " -> " + string.Join(", ", names.ToArray());
        }

        return line;
    }
}
