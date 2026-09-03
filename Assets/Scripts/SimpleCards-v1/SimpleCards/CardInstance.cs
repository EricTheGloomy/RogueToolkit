// ONE PHYSICAL COPY OF A CARD - the actual bit of cardboard.
//
// This is the thing that matters most for a card game and the thing people
// most often skip: a deck with three Strikes in it has ONE Strike asset and
// THREE CardInstances. Without that, you cannot tell one Strike in your hand
// from another, so you cannot discard "that one", upgrade "this one", or
// animate them separately.
//
// It is a plain class, not a ScriptableObject, because it is created and thrown
// away constantly while playing.

public class CardInstance
{
    // Which kind of card this is. Cast it to reach your own fields:
    //     AttackCard attack = (AttackCard)instance.card;
    public readonly Card card;

    // Tells two copies of the same card apart. Handy in logs, and as a key if
    // your UI keeps a dictionary of card objects.
    public readonly int instanceId;

    // Per-copy cost change, so "this copy costs 1 less this fight" works
    // without touching the shared asset. Negative makes it cheaper.
    public int costChange = 0;

    private static int nextInstanceId = 1;

    public CardInstance(Card card)
    {
        this.card = card;
        instanceId = nextInstanceId;
        nextInstanceId++;
    }

    // Never negative, however much you discount it.
    public int GetCost()
    {
        if (card == null) return 0;

        int total = card.cost + costChange;

        return (total < 0) ? 0 : total;
    }

    public string GetDisplayName()
    {
        return (card != null) ? card.GetDisplayName() : "<empty card>";
    }

    public bool HasTag(string tag)
    {
        return (card != null) && card.HasTag(tag);
    }

    // "Strike#7" - the name plus which copy, for logs.
    public override string ToString()
    {
        return GetDisplayName() + "#" + instanceId;
    }
}
