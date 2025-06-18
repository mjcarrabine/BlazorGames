namespace CardGamesLibrary.Shared
{
    public class TableCard
    {
        public Card Card { get; }
        public bool IsFaceUp { get; }

        public TableCard(Card card, bool isFaceUp)
        {
            Card = card;
            IsFaceUp = isFaceUp;
        }
    }
}