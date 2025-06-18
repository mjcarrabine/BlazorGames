namespace CardGamesLibrary.Shared
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }

    public static class SuitExtensions
    {
        public static string ToSymbol(this Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => "<span class=\"card-suit\">♣</span>",
                Suit.Diamonds => "<span class=\"card-suit red\">♦</span>",
                Suit.Hearts => "<span class=\"card-suit red\">♥</span>",
                Suit.Spades => "<span class=\"card-suit\">♠</span>",
                _ => "?"
            };
        }
    }

    public enum Rank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

    public class Card
    {
        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
        // public override string ToString() => $"{Rank} of {Suit}";

         public override string ToString()
        {
            string rankStr = Rank switch
            {
                Rank.Two => "2",
                Rank.Three => "3",
                Rank.Four => "4",
                Rank.Five => "5",
                Rank.Six => "6",
                Rank.Seven => "7",
                Rank.Eight => "8",
                Rank.Nine => "9",
                Rank.Ten => "10",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                Rank.Ace => "A",
                _ => ((int)Rank).ToString()
            };
            return $"{rankStr} {Suit.ToSymbol()}";
        }
    }

    public class Deck
    {
        private List<Card> cards = new();
        private Random rng = new();
        public Deck()
        {
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                    cards.Add(new Card(suit, rank));
        }
        public void Shuffle() => cards = cards.OrderBy(_ => rng.Next()).ToList();

        // TODO: decide between Deal and Draw
        // Deal is more intuitive for card games, Draw is more generic
        public Card? Deal()
        {
            return Draw();
        }
        public Card? Draw()
        {
            if (cards.Count == 0) return null;
            var card = cards[0];
            cards.RemoveAt(0);
            return card;
        }
        public int Count => cards.Count;
    }
}