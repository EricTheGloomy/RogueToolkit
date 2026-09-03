using System.Collections.Generic;
using UnityEngine;

// A THROWAWAY VIEW for the SimpleCards kit, so you can actually PLAY a turn
// instead of reading one in the Console.
//
// HOW TO USE: add this to any empty GameObject and press Play. That is all -
// no Canvas, no prefabs, no scene setup, no art.
//
//   click a card in hand        select it (targeted cards wait for a target)
//   click an enemy              play the selected card at it
//   click the card again        cancel
//   End Turn button             discard your hand and draw a fresh one
//
// Read the message line. Every refusal the kit produces is printed there, which
// is the fastest way to understand the rules.
//
//
// WHY IS THIS OnGUI AND NOT PROPER UNITY UI?
//
// On purpose - see the same note in GridPrototypeView. Short version: no Canvas,
// no prefabs, no font packages, and it cannot be mistaken for your real UI.
//
// When you rebuild this with uGUI, note what changes in the kit: NOTHING. This
// file talks to CardTable exactly the way your real UI will.
//
// Delete this file when you are done with it.

public class CardPrototypeView : MonoBehaviour
{
    [Tooltip("Leave empty to use the built-in demo deck.")]
    public CardLibrary library;

    [Header("Look")]
    public int cardWidth = 110;
    public int cardHeight = 150;

    // ---- state ----
    private CardTable table;
    private CardInstance selected;
    private string message = "Click a card to play it.";

    private Texture2D dot;

    // Stand-ins for your real enemies. In your game these are MonoBehaviours
    // with health, animation and a transform.
    private class DemoEnemy
    {
        public string name;
        public int health;
        public int maxHealth;
    }

    private List<DemoEnemy> enemies = new List<DemoEnemy>();
    private int block;

    private static readonly Color background = new Color(0.13f, 0.14f, 0.17f);
    private static readonly Color cardFace = new Color(0.93f, 0.92f, 0.89f);
    private static readonly Color cardCheap = new Color(0.80f, 0.86f, 0.93f);
    private static readonly Color cardUnaffordable = new Color(0.55f, 0.54f, 0.53f);
    private static readonly Color cardSelected = new Color(0.99f, 0.85f, 0.45f);
    private static readonly Color enemyBox = new Color(0.56f, 0.28f, 0.28f);
    private static readonly Color enemyLit = new Color(0.88f, 0.48f, 0.42f);
    private static readonly Color barBack = new Color(0.22f, 0.20f, 0.20f);
    private static readonly Color barFill = new Color(0.45f, 0.75f, 0.45f);

    void Start()
    {
        table = (library != null) ? library.StartNewGame() : BuildDemoTable();

        enemies.Add(new DemoEnemy { name = "Goblin", health = 14, maxHealth = 14 });
        enemies.Add(new DemoEnemy { name = "Wolf", health = 9, maxHealth = 9 });
        enemies.Add(new DemoEnemy { name = "Bandit", health = 20, maxHealth = 20 });

        RefreshTargets();
        StartTurn();
    }

    void StartTurn()
    {
        table.RefillEnergy();
        block = 0;
        int drawn = table.Draw(5);
        selected = null;
        message = "Drew " + drawn + " cards.";
    }

    // Your game calls something like this whenever the board changes. Getting
    // it wrong is how a card ends up being played on a corpse.
    void RefreshTargets()
    {
        table.availableTargets.Clear();

        foreach (DemoEnemy enemy in enemies)
        {
            table.availableTargets.Add(new CardTarget("enemy", enemy.name, null));
        }
    }

    void OnGUI()
    {
        EnsureDot();

        Box(new Rect(0, 0, Screen.width, Screen.height), background);

        DrawEnemies();
        DrawStatus();
        DrawHand();
        DrawButtons();

        GUI.Label(new Rect(20, Screen.height - 26, Screen.width - 40, 22), message);
    }

    // ---------------- drawing ----------------

    void DrawEnemies()
    {
        // A targeted card is waiting for a click, so light up what it can hit.
        bool waiting = selected != null && NeedsAClickedTarget(selected);

        for (int i = 0; i < enemies.Count; i++)
        {
            DemoEnemy enemy = enemies[i];
            Rect area = new Rect(40 + i * 150, 60, 130, 74);

            Box(area, waiting ? enemyLit : enemyBox);

            GUI.Label(new Rect(area.x + 8, area.y + 6, 120, 20), enemy.name);
            GUI.Label(new Rect(area.x + 8, area.y + 44, 120, 20), enemy.health + " / " + enemy.maxHealth);

            // Health bar.
            Rect bar = new Rect(area.x + 8, area.y + 30, 114, 10);
            Box(bar, barBack);
            float fraction = (enemy.maxHealth > 0) ? (float)enemy.health / enemy.maxHealth : 0f;
            if (fraction > 0f) Box(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), barFill);

            if (waiting && GUI.Button(new Rect(area.x, area.y + 78, 130, 24), "Target"))
            {
                PlayAt(enemy);
            }
        }

        if (enemies.Count == 0)
        {
            GUI.Label(new Rect(40, 70, 400, 22), "All enemies dead. Targeted cards will be refused.");
        }
    }

    void DrawStatus()
    {
        int right = Screen.width - 220;

        GUI.Label(new Rect(right, 20, 200, 22), "Energy   " + table.energy + " / " + table.maxEnergy);
        GUI.Label(new Rect(right, 40, 200, 22), "Block    " + block);
        GUI.Label(new Rect(right, 68, 200, 22), "Draw pile      " + table.drawPile.Count);
        GUI.Label(new Rect(right, 88, 200, 22), "Discard pile   " + table.discardPile.Count);
        GUI.Label(new Rect(right, 108, 200, 22), "Exiled         " + table.exilePile.Count);
    }

    void DrawHand()
    {
        List<CardInstance> hand = table.hand.GetAll();

        int totalWidth = hand.Count * (cardWidth + 8);
        int left = (Screen.width - totalWidth) / 2;
        int top = Screen.height - cardHeight - 60;

        for (int i = 0; i < hand.Count; i++)
        {
            CardInstance card = hand[i];
            Rect area = new Rect(left + i * (cardWidth + 8), top, cardWidth, cardHeight);

            bool affordable = table.CanAfford(card);
            Color face = affordable ? (card.GetCost() == 0 ? cardCheap : cardFace) : cardUnaffordable;

            if (card == selected)
            {
                // A fat border rather than a glow - it is a prototype.
                Box(new Rect(area.x - 4, area.y - 4, area.width + 8, area.height + 8), cardSelected);
                area = new Rect(area.x, area.y - 14, area.width, area.height);
            }

            Box(area, face);

            GUI.color = Color.black;
            GUI.Label(new Rect(area.x + 6, area.y + 4, 30, 20), card.GetCost().ToString());
            GUI.Label(new Rect(area.x + 6, area.y + 24, cardWidth - 12, 40), card.GetDisplayName());
            GUI.Label(new Rect(area.x + 6, area.y + 62, cardWidth - 12, 80), card.card.description);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(area.x, area.y + area.height - 26, cardWidth, 24), "Play"))
            {
                OnCardClicked(card);
            }
        }

        if (hand.Count == 0)
        {
            GUI.Label(new Rect(Screen.width / 2 - 60, top + 60, 200, 22), "hand is empty");
        }
    }

    void DrawButtons()
    {
        if (GUI.Button(new Rect(Screen.width - 130, Screen.height - 60, 110, 30), "End Turn"))
        {
            int dumped = table.DiscardHand();
            EnemiesAttack();
            RefreshTargets();
            StartTurn();
            message = "Discarded " + dumped + " and drew a fresh hand.";
        }

        if (GUI.Button(new Rect(20, 20, 110, 26), "Restart"))
        {
            Start();
        }
    }

    // ---------------- the bit you would actually write in your game ----------

    void OnCardClicked(CardInstance card)
    {
        // Clicking the selected card again cancels.
        if (selected == card)
        {
            selected = null;
            message = "Cancelled.";
            return;
        }

        if (!table.CanAfford(card))
        {
            message = "Not enough energy for " + card.GetDisplayName() + ".";
            return;
        }

        if (NeedsAClickedTarget(card))
        {
            selected = card;
            message = "Now click a " + card.card.targetKind + ".";
            return;
        }

        // Needs nothing picked, so play it now.
        Resolve(table.Play(card, null), null);
    }

    void PlayAt(DemoEnemy enemy)
    {
        if (selected == null) return;

        CardTarget target = FindTarget(enemy.name);

        List<CardTarget> chosen = new List<CardTarget>();
        if (target != null) chosen.Add(target);

        Resolve(table.Play(selected, chosen), enemy);
        selected = null;
    }

    void Resolve(PlayResult result, DemoEnemy clicked)
    {
        if (!result.played)
        {
            // Nothing changed, so just say why and carry on.
            message = result.refusedBecause;
            return;
        }

        message = "Played " + result.card.GetDisplayName() + ".";
        ApplyEffect(result);
    }

    void ApplyEffect(PlayResult result)
    {
        DemoCard details = result.card.card as DemoCard;

        if (details == null)
        {
            message += "  (your own card type - apply its effect in ApplyEffect)";
            return;
        }

        bool somebodyDied = false;

        foreach (CardTarget target in result.targets)
        {
            DemoEnemy enemy = FindEnemy(target.GetName());
            if (enemy == null) continue;

            enemy.health -= details.damage;

            if (enemy.health <= 0)
            {
                enemies.Remove(enemy);
                somebodyDied = true;
            }
        }

        // IMPORTANT: rebuild the target list whenever something dies, or a
        // later card could be played on a corpse.
        if (somebodyDied) RefreshTargets();

        block += details.block;

        if (details.drawCards > 0)
        {
            int extra = table.Draw(details.drawCards);
            message += "  Drew " + extra + ".";
        }

        if (result.card.card.customTag != "")
        {
            message += "  [" + result.card.card.customTag + "]";
        }
    }

    void EnemiesAttack()
    {
        // Just enough to make block worth having.
        int incoming = enemies.Count * 4;
        int through = incoming - block;

        if (through < 0) through = 0;

        if (enemies.Count > 0)
        {
            message = "Enemies hit for " + incoming + ", " + block + " blocked, " + through + " through.";
        }
    }

    // ---------------- helpers ----------------

    bool NeedsAClickedTarget(CardInstance card)
    {
        // All-targeting cards need no click - the kit works them out itself.
        return card.card.targeting == Card.Targeting.ChooseOne
            || card.card.targeting == Card.Targeting.ChooseMany;
    }

    CardTarget FindTarget(string targetName)
    {
        foreach (CardTarget target in table.availableTargets)
        {
            if (target.GetName() == targetName) return target;
        }
        return null;
    }

    DemoEnemy FindEnemy(string enemyName)
    {
        foreach (DemoEnemy enemy in enemies)
        {
            if (enemy.name == enemyName) return enemy;
        }
        return null;
    }

    void Box(Rect area, Color colour)
    {
        Color was = GUI.color;
        GUI.color = colour;
        GUI.DrawTexture(area, dot);
        GUI.color = was;
    }

    void EnsureDot()
    {
        if (dot != null) return;

        dot = new Texture2D(1, 1);
        dot.SetPixel(0, 0, Color.white);
        dot.Apply();
    }

    // ---------------- the demo deck ----------------
    // Only used when you leave the library empty.

    private class DemoCard : Card
    {
        public int damage;
        public int block;
        public int drawCards;
    }

    CardTable BuildDemoTable()
    {
        DemoCard strike = MakeCard("Strike", 1, Card.Targeting.ChooseOne, "Deal 6 damage.");
        strike.damage = 6;

        DemoCard defend = MakeCard("Defend", 1, Card.Targeting.None, "Gain 5 block.");
        defend.block = 5;

        DemoCard cleave = MakeCard("Cleave", 2, Card.Targeting.All, "Deal 4 to ALL enemies.");
        cleave.damage = 4;
        cleave.customTag = "SCREEN_SHAKE";

        DemoCard plan = MakeCard("Plan Ahead", 0, Card.Targeting.None, "Draw 2. Exiles itself.");
        plan.drawCards = 2;
        plan.afterPlay = Card.AfterPlay.Exile;

        DemoCard heavy = MakeCard("Heavy Blow", 3, Card.Targeting.ChooseOne, "Deal 14 damage.");
        heavy.damage = 14;

        List<Card> deck = new List<Card>();
        for (int i = 0; i < 5; i++) deck.Add(strike);
        for (int i = 0; i < 4; i++) deck.Add(defend);
        deck.Add(cleave);
        deck.Add(cleave);
        deck.Add(plan);
        deck.Add(heavy);

        CardTable built = new CardTable();
        built.maxEnergy = 3;
        built.StartNewGame(deck);
        return built;
    }

    DemoCard MakeCard(string cardName, int cost, Card.Targeting targeting, string description)
    {
        DemoCard card = ScriptableObject.CreateInstance<DemoCard>();
        card.name = cardName;
        card.displayName = cardName;
        card.cost = cost;
        card.targeting = targeting;
        card.targetKind = "enemy";
        card.description = description;
        return card;
    }
}
