using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGamesLibrary.Shared
{
    public enum UnoColor { Red, Yellow, Green, Blue, Wild }
    public enum UnoValue { Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Skip, Reverse, DrawTwo, Wild, WildDrawFour }

    public class UnoCard
    {
        public UnoColor Color { get; set; }
        public UnoValue Value { get; set; }
        public override string ToString() => Color == UnoColor.Wild ? $"{Value}" : $"{Color} {Value}";
    }

    public class UnoGameService
    {
        public List<UnoCard> PlayerHand { get; set; } = new();
        public List<UnoCard> ComputerHand { get; set; } = new();
        public Stack<UnoCard> DrawPile { get; set; } = new();
        public Stack<UnoCard> DiscardPile { get; set; } = new();
        public UnoCard? TopCard => DiscardPile.Count > 0 ? DiscardPile.Peek() : null;
        public bool GameStarted { get; set; } = false;
        public string Message { get; set; } = "";
        public bool PlayerTurn { get; set; } = true;
        private Random rng = new();
        private UnoColor? activeWildColor = null;
        public UnoColor? ActiveWildColor => activeWildColor;

        public void StartGame()
        {
            PlayerHand.Clear();
            ComputerHand.Clear();
            DrawPile.Clear();
            DiscardPile.Clear();
            Message = "";
            PlayerTurn = true;
            var deck = GenerateDeck();
            foreach (var card in deck.OrderBy(_ => rng.Next()))
                DrawPile.Push(card);
            for (int i = 0; i < 7; i++)
            {
                PlayerHand.Add(DrawPile.Pop());
                ComputerHand.Add(DrawPile.Pop());
            }
            DiscardPile.Push(DrawPile.Pop());
            GameStarted = true;
        }

        public List<UnoCard> GenerateDeck()
        {
            var deck = new List<UnoCard>();
            foreach (UnoColor color in Enum.GetValues(typeof(UnoColor)))
            {
                if (color == UnoColor.Wild) continue;
                deck.Add(new UnoCard { Color = color, Value = UnoValue.Zero });
                for (int i = 0; i < 2; i++)
                {
                    for (UnoValue v = UnoValue.One; v <= UnoValue.DrawTwo; v++)
                        deck.Add(new UnoCard { Color = color, Value = v });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                deck.Add(new UnoCard { Color = UnoColor.Wild, Value = UnoValue.Wild });
                deck.Add(new UnoCard { Color = UnoColor.Wild, Value = UnoValue.WildDrawFour });
            }
            return deck;
        }

        public void PlayCard(UnoCard card, UnoColor? chosenColor = null)
        {
            if (!PlayerTurn || !CanPlay(card))
            {
                Message = "Invalid move.";
                return;
            }
            PlayerHand.Remove(card);
            DiscardPile.Push(card);
            if (card.Color == UnoColor.Wild)
            {
                if (chosenColor == null)
                {
                    PendingWildColor = null;
                    AwaitingWildColor = true;
                    Message = "Select a color for your Wild card.";
                    return;
                }
                PendingWildColor = chosenColor;
                activeWildColor = chosenColor;
                AwaitingWildColor = false;
                Message = $"You played {card} and chose {chosenColor}.";
            }
            else
            {
                PendingWildColor = null;
                activeWildColor = null;
                AwaitingWildColor = false;
                Message = $"You played {card}.";
            }
            // Handle Skip card
            if (card.Value == UnoValue.Skip)
            {
                Message += " The next player's turn is skipped.";
                PlayerTurn = false;
                ComputerMove();
                return;
            }
            // Handle Draw Two card
            if (card.Value == UnoValue.DrawTwo)
            {
                Message += " The next player must draw 2 cards.";
                PlayerTurn = false;
                ComputerDrawCards(2);
                ComputerMove();
                return;
            }
            PlayerTurn = false;
            ComputerMove();
        }

        private void ComputerDrawCards(int count)
        {
            for (int i = 0; i < count && DrawPile.Count > 0; i++)
            {
                ComputerHand.Add(DrawPile.Pop());
            }
            Message += $" Computer drew {count} card(s).";
        }

        public bool CanPlay(UnoCard card)
        {
            if (TopCard == null) return false;
            if (activeWildColor != null)
            {
                return card.Color == activeWildColor || card.Color == UnoColor.Wild;
            }
            return card.Color == TopCard.Color || card.Value == TopCard.Value || card.Color == UnoColor.Wild;
        }
        public UnoColor? PendingWildColor { get; set; } = null;
        public bool AwaitingWildColor { get; set; } = false;
        public void ComputerMove()
        {
            var playable = ComputerHand.FirstOrDefault(CanPlay);
            if (playable != null)
            {
                ComputerHand.Remove(playable);
                DiscardPile.Push(playable);
                if (playable.Color == UnoColor.Wild)
                {
                    var color = GetMostCommonColor(ComputerHand);
                    PendingWildColor = color;
                    activeWildColor = color;
                    Message += $" Computer played {playable} and chose {color}.";
                }
                else
                {
                    PendingWildColor = null;
                    activeWildColor = null;
                    Message += $" Computer played {playable}.";
                }
                // Handle Skip card for computer
                if (playable.Value == UnoValue.Skip)
                {
                    Message += " Your turn is skipped.";
                    PlayerTurn = true;
                    return;
                }
                // Handle Draw Two card for computer
                if (playable.Value == UnoValue.DrawTwo)
                {
                    Message += " You must draw 2 cards.";
                    PlayerDrawCards(2);
                    PlayerTurn = true;
                    return;
                }
                PlayerTurn = true;
                return;
            }
            else if (DrawPile.Count > 0)
            {
                var drawnCard = DrawPile.Pop();
                ComputerHand.Add(drawnCard);
                Message += $" Computer drew a card.";
                if (CanPlay(drawnCard))
                {
                    ComputerHand.Remove(drawnCard);
                    DiscardPile.Push(drawnCard);
                    Message += $" Computer played {drawnCard}.";
                    if (drawnCard.Color == UnoColor.Wild)
                    {
                        var color = GetMostCommonColor(ComputerHand);
                        PendingWildColor = color;
                        activeWildColor = color;
                        Message += $" Computer chose {color}.";
                    }
                    else
                    {
                        PendingWildColor = null;
                        activeWildColor = null;
                    }
                    // Handle Skip card for computer
                    if (drawnCard.Value == UnoValue.Skip)
                    {
                        Message += " Your turn is skipped.";
                        PlayerTurn = true;
                        return;
                    }
                    // Handle Draw Two card for computer
                    if (drawnCard.Value == UnoValue.DrawTwo)
                    {
                        Message += " You must draw 2 cards.";
                        PlayerDrawCards(2);
                        PlayerTurn = true;
                        return;
                    }
                    PlayerTurn = true;
                    return;
                }
            }
            PlayerTurn = true;
        }
        public void PlayerDrawCards(int count)
        {
            for (int i = 0; i < count && DrawPile.Count > 0; i++)
            {
                PlayerHand.Add(DrawPile.Pop());
            }
            Message += $" You drew {count} card(s).";
        }

        public IEnumerable<UnoCard> GetSortedPlayerHand() =>
            PlayerHand.OrderBy(c => c.Color).ThenBy(c => c.Value);
        public IEnumerable<UnoCard> GetSortedComputerHand() =>
            ComputerHand.OrderBy(c => c.Color).ThenBy(c => c.Value);
        public UnoColor GetMostCommonColor(IEnumerable<UnoCard> hand)
        {
            return hand.Where(c => c.Color != UnoColor.Wild)
                .GroupBy(c => c.Color)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
        }
    }
}
