using System.Collections.Generic;
using System.Linq;

namespace CardGamesLibrary.Shared
{
    public class BlackjackGameService
    {
        public Deck? Deck { get; set; }
        public List<Card> PlayerHand { get; set; } = new();
        public List<Card> DealerHand { get; set; } = new();
        public bool GameStarted { get; set; } = false;

        public void StartGame()
        {
            Deck = new Deck();
            Deck.Shuffle();
            PlayerHand.Clear();
            DealerHand.Clear();
            var card = Deck.Draw();
            if (card != null) PlayerHand.Add(card);
            card = Deck.Draw();
            if (card != null) DealerHand.Add(card);
            card = Deck.Draw();
            if (card != null) PlayerHand.Add(card);
            card = Deck.Draw();
            if (card != null) DealerHand.Add(card);
            GameStarted = true;
        }

        public string GetBasicStrategyRecommendation()
        {
            if (PlayerHand.Count < 2 || DealerHand.Count < 1)
                return "";

            int playerTotal = PlayerHand.Sum(card => card.Rank <= Rank.Ten ? (int)card.Rank : 10);
            int dealerUpCard = DealerHand[0].Rank <= Rank.Ten ? (int)DealerHand[0].Rank : 10;

            // Simple basic strategy (expand as needed)
            if (playerTotal <= 11)
                return "Hit";
            if (playerTotal >= 17)
                return "Stand";
            if (playerTotal >= 12 && playerTotal <= 16)
                return dealerUpCard >= 7 ? "Hit" : "Stand";
            return "";
        }

        public void PlayerHit()
        {
            if (Deck != null && GameStarted)
            {
                var card = Deck.Draw();
                if (card != null) PlayerHand.Add(card);
            }
        }

        public void PlayerStand()
        {
            if (Deck == null || !GameStarted)
                return;

            // Dealer reveals hidden card and plays
            while (GetHandValue(DealerHand) < 17)
            {
                var card = Deck.Draw();
                if (card != null) DealerHand.Add(card);
            }
            GameStarted = false;
        }

        public int GetHandValue(List<Card> hand)
        {
            int value = 0;
            int aceCount = 0;
            foreach (var card in hand)
            {
                int cardValue = (int)card.Rank;
                if (card.Rank >= Rank.Jack && card.Rank <= Rank.King)
                    cardValue = 10;
                if (card.Rank == Rank.Ace)
                {
                    cardValue = 11;
                    aceCount++;
                }
                value += cardValue;
            }
            // Adjust for aces
            while (value > 21 && aceCount > 0)
            {
                value -= 10;
                aceCount--;
            }
            return value;
        }

        public string GetGameResult()
        {
            int playerValue = GetHandValue(PlayerHand);
            int dealerValue = GetHandValue(DealerHand);
            if (playerValue > 21)
                return "You bust! Dealer wins.";
            if (dealerValue > 21)
                return "Dealer busts! You win!";
            if (!GameStarted)
            {
                if (playerValue > dealerValue)
                    return "You win!";
                if (playerValue < dealerValue)
                    return "Dealer wins.";
                return "Push (tie).";
            }
            return string.Empty;
        }
    }
}