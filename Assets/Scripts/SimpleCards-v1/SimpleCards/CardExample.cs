using System.Collections.Generic;
using UnityEngine;

// A throwaway demo. Put it on any GameObject and press Play, then read the
// Console. It builds a small deck and two enemies in code, so you see a whole
// turn work with zero setup.
//
// Delete this file once it clicks. Nothing else needs it.

public class CardExample : MonoBehaviour
{
    [Tooltip("Optional. Leave empty to run the built-in demo deck.")]
    public CardLibrary library;

    private CardTable table;

    // Stand-ins for your real enemies. In a game these would be MonoBehaviours
    // with health and animation; here they are just names and numbers.
    private class FakeEnemy
    {
        public string name;
        public int health;
    }

    private List<FakeEnemy> enemies = new List<FakeEnemy>();

    void Start()
    {
        // Seeded so the demo prints the same thing every run. Delete this line
        // in a real game.
        UnityEngine.Random.InitState(7);

        table = (library != null) ? library.StartNewGame() : BuildDemoTable();

        enemies.Add(new FakeEnemy { name = "Goblin", health = 10 });
        enemies.Add(new FakeEnemy { name = "Wolf", health = 6 });
        RefreshTargets();

        // ---- turn one ----------------------------------------------------
        Debug.Log("=== TURN 1 ===");
        int drawn = table.Draw(5);
        Debug.Log("Drew " + drawn + " cards.\n" + table.Describe());

        // Show what refusals look like, while the hand is still full.
        ShowSomeRefusals();

        // Then play until the energy runs out.
        PlayFirstPlayable();
        PlayFirstPlayable();
        PlayFirstPlayable();
        PlayFirstPlayable();

        Debug.Log("End of turn 1.\n" + table.Describe());

        // ---- turn two: end turn, redraw ----------------------------------
        Debug.Log("=== TURN 2 ===");
        int dumped = table.DiscardHand();
        table.RefillEnergy();
        Debug.Log("Discarded " + dumped + " unplayed cards and refilled energy.");

        drawn = table.Draw(5);
        Debug.Log("Drew " + drawn + " cards.\n" + table.Describe());

        // ---- turn three: watch the reshuffle ------------------------------
        Debug.Log("=== TURN 3: draw pile should run dry and reshuffle ===");
        table.DiscardHand();
        table.RefillEnergy();
        drawn = table.Draw(5);
        Debug.Log("Drew " + drawn + " cards.\n" + table.Describe());

        Debug.Log("Enemies left: " + DescribeEnemies());
    }

    // ---- the bit you would actually write in your game -----------------------

    // Finds the first card in hand that can be afforded, picks a target for it,
    // and plays it. Your game would wait for the player to click instead.
    void PlayFirstPlayable()
    {
        foreach (CardInstance card in table.hand.GetAll())
        {
            if (!table.CanAfford(card)) continue;

            // Pick targets. In a real game this is the player clicking an enemy.
            List<CardTarget> chosen = null;

            if (card.card.targeting == Card.Targeting.ChooseOne)
            {
                List<CardTarget> options = table.GetTargetsOfKind(card.card.targetKind);
                if (options.Count == 0) continue; // nothing to hit, try another card

                chosen = new List<CardTarget>();
                chosen.Add(options[0]);
            }

            PlayResult result = table.Play(card, chosen);

            if (!result.played)
            {
                Debug.Log("  could not play " + card.GetDisplayName() + ": " + result.refusedBecause);
                continue;
            }

            Debug.Log("  PLAYED " + result.ToString() + "   (energy left " + table.energy + ")");

            // THIS is where your game does the thing. The kit told us what was
            // played and what it hit; the effect is ours to apply.
            ApplyEffect(result);
            return;
        }

        Debug.Log("  nothing playable in hand.");
    }

    void ApplyEffect(PlayResult result)
    {
        // "as" rather than a straight cast, because if you plugged your own
        // library in, these are your card type and not DemoCard. A straight
        // cast would throw; this just does nothing.
        DemoCard details = result.card.card as DemoCard;

        if (details == null)
        {
            Debug.Log("      (your own card type - apply its effect here)");
            return;
        }

        if (details.damage > 0)
        {
            foreach (CardTarget target in result.targets)
            {
                FakeEnemy enemy = FindEnemy(target.GetName());
                if (enemy == null) continue;

                enemy.health -= details.damage;
                Debug.Log("      " + enemy.name + " takes " + details.damage
                          + " damage (" + enemy.health + " hp left)");

                if (enemy.health <= 0)
                {
                    Debug.Log("      " + enemy.name + " dies");
                    enemies.Remove(enemy);

                    // IMPORTANT: rebuild the target list whenever something
                    // dies, or a later card could be played on a corpse.
                    RefreshTargets();
                }
            }
        }

        if (details.block > 0)
        {
            Debug.Log("      gained " + details.block + " block");
        }

        if (details.drawCards > 0)
        {
            int extra = table.Draw(details.drawCards);
            Debug.Log("      drew " + extra + " more cards");
        }

        // The escape hatch, same idea as the other kits.
        if (result.card.card.customTag != "")
        {
            Debug.Log("      custom tag fired: " + result.card.card.customTag);
        }
    }

    // Your game calls something like this whenever the board changes.
    void RefreshTargets()
    {
        table.availableTargets.Clear();

        foreach (FakeEnemy enemy in enemies)
        {
            // The third argument is the real object behind the target. Here we
            // have no Unity object to pass, so it is null and we match by name.
            // In your game you would pass the enemy's script itself.
            table.availableTargets.Add(new CardTarget("enemy", enemy.name, null));
        }
    }

    FakeEnemy FindEnemy(string enemyName)
    {
        foreach (FakeEnemy enemy in enemies)
        {
            if (enemy.name == enemyName) return enemy;
        }
        return null;
    }

    string DescribeEnemies()
    {
        if (enemies.Count == 0) return "none";

        List<string> parts = new List<string>();
        foreach (FakeEnemy enemy in enemies) parts.Add(enemy.name + " " + enemy.health + "hp");
        return string.Join(", ", parts.ToArray());
    }

    // ---- deliberately showing refusals -------------------------------------

    // Every refusal comes with a sentence you can put straight on screen.
    // Nothing changes on a refusal, so trying is always safe.
    void ShowSomeRefusals()
    {
        Debug.Log("  -- on purpose, four things that get refused --");

        // 1. A card that is not in your hand at all.
        CardInstance notMine = new CardInstance(table.hand.GetAt(0).card);
        Debug.Log("  " + table.Play(notMine, null).refusedBecause);

        // 2. A targeted card with nothing picked.
        foreach (CardInstance card in table.hand.GetAll())
        {
            if (card.card.targeting == Card.Targeting.ChooseOne)
            {
                Debug.Log("  " + table.Play(card, null).refusedBecause);
                break;
            }
        }

        // 3. Pointing at something that is not on the target list - a corpse.
        foreach (CardInstance card in table.hand.GetAll())
        {
            if (card.card.targeting == Card.Targeting.ChooseOne)
            {
                List<CardTarget> ghost = new List<CardTarget>();
                ghost.Add(new CardTarget("enemy", "A Dead Goblin", null));
                Debug.Log("  " + table.Play(card, ghost).refusedBecause);
                break;
            }
        }

        // 4. Anything, with no energy. Put the energy back afterwards.
        int saved = table.energy;
        table.energy = 0;
        Debug.Log("  " + table.Play(table.hand.GetAt(0), null).refusedBecause);
        table.energy = saved;

        Debug.Log("  (hand is still " + table.hand.Count + " cards - refusals change nothing)");
    }

    // ---- the demo deck ------------------------------------------------------
    // In a real project these are assets you make in the Project window. Built
    // in code here purely so the demo needs no setup.

    // Your own card type, with your own fields. This is the pattern to copy.
    private class DemoCard : Card
    {
        public int damage;
        public int block;
        public int drawCards;
    }

    CardTable BuildDemoTable()
    {
        DemoCard strike = MakeCard("Strike", 1, Card.Targeting.ChooseOne);
        strike.damage = 6;
        strike.description = "Deal 6 damage.";

        DemoCard defend = MakeCard("Defend", 1, Card.Targeting.None);
        defend.block = 5;
        defend.description = "Gain 5 block.";

        DemoCard cleave = MakeCard("Cleave", 2, Card.Targeting.All);
        cleave.damage = 4;
        cleave.description = "Deal 4 damage to ALL enemies.";
        cleave.customTag = "SCREEN_SHAKE";

        DemoCard plan = MakeCard("Plan Ahead", 0, Card.Targeting.None);
        plan.drawCards = 2;
        plan.description = "Draw 2 cards. Exiles itself.";
        plan.afterPlay = Card.AfterPlay.Exile;

        // A 10-card deck: four Strikes, four Defends, one of each other.
        // The same asset appearing several times is exactly how copies work.
        List<Card> deck = new List<Card>();
        for (int i = 0; i < 4; i++) deck.Add(strike);
        for (int i = 0; i < 4; i++) deck.Add(defend);
        deck.Add(cleave);
        deck.Add(plan);

        CardTable newTable = new CardTable();
        newTable.maxEnergy = 3;
        newTable.StartNewGame(deck);

        Debug.Log("Built a " + deck.Count + "-card demo deck.");

        return newTable;
    }

    DemoCard MakeCard(string cardName, int cost, Card.Targeting targeting)
    {
        DemoCard card = ScriptableObject.CreateInstance<DemoCard>();
        card.name = cardName;
        card.displayName = cardName;
        card.cost = cost;
        card.targeting = targeting;
        card.targetKind = "enemy";
        return card;
    }
}
