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
            // Ensure the first top card is not an action or wild card
            UnoCard firstCard;
            int safetyCounter = 0;
            const int maxAttempts = 100;
            do
            {
                if (++safetyCounter > maxAttempts)
                {
                    throw new InvalidOperationException("Unable to find a valid starting card for Uno after multiple attempts. The deck may be malformed.");
                }
                firstCard = DrawPile.Pop();
                // If not allowed, put it at the bottom and try again
                if (firstCard.Value == UnoValue.Skip || firstCard.Value == UnoValue.Reverse ||
                    firstCard.Value == UnoValue.DrawTwo || firstCard.Value == UnoValue.Wild || firstCard.Value == UnoValue.WildDrawFour)
                {
                    DrawPile = new Stack<UnoCard>(DrawPile.Reverse().Append(firstCard));
                }
                else
                {
                    break;
                }
            } while (true);
            DiscardPile.Push(firstCard);
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
            if (!GameStarted) return;
            if (!PlayerTurn || !CanPlay(card))
            {
                Message = "Invalid move.";
                return;
            }
            PlayerHand.Remove(card);
            DiscardPile.Push(card);
            if (PlayerHand.Count == 0)
            {
                Message = "You win!";
                GameStarted = false;
                return;
            }
            if (card.Color == UnoColor.Wild)
            {
                if (card.Value == UnoValue.Wild || card.Value == UnoValue.WildDrawFour)
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
                    if (card.Value == UnoValue.WildDrawFour)
                    {
                        Message += " The next player must draw 4 cards.";
                        PlayerTurn = false;
                        ComputerDrawCards(4);
                        if (ComputerHand.Count == 0)
                        {
                            Message = "Computer wins!";
                            GameStarted = false;
                            return;
                        }
                        ComputerMove();
                        return;
                    }
                    // For normal Wild, after color selection, just pass turn
                    PlayerTurn = false;
                    if (ComputerHand.Count == 0)
                    {
                        Message = "Computer wins!";
                        GameStarted = false;
                        return;
                    }
                    ComputerMove();
                    return;
                }
            }
            else
            {
                PendingWildColor = null;
                activeWildColor = null;
                AwaitingWildColor = false;
                Message = $"You played {card}.";
            }
            // Handle Skip card
            if (card.Value == UnoValue.Skip || card.Value == UnoValue.Reverse)
            {
                Message += card.Value == UnoValue.Skip ? " The next player's turn is skipped." : " Reverse acts as Skip in a 2-player game.";
                PlayerTurn = true;
                return;
            }
            // Handle Draw Two card
            if (card.Value == UnoValue.DrawTwo)
            {
                Message += " The next player must draw 2 cards.";
                PlayerTurn = false;
                ComputerDrawCards(2);
                if (ComputerHand.Count == 0)
                {
                    Message = "Computer wins!";
                    GameStarted = false;
                    return;
                }
                ComputerMove();
                return;
            }
            PlayerTurn = false;
            if (ComputerHand.Count == 0)
            {
                Message = "Computer wins!";
                GameStarted = false;
                return;
            }
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
                // Only allow WildDrawFour if no other playable cards
                if (card.Value == UnoValue.WildDrawFour)
                {
                    return !PlayerHand.Any(c => c != card && c.Color != UnoColor.Wild && (c.Color == activeWildColor || c.Value == TopCard.Value));
                }
                return card.Color == activeWildColor || card.Color == UnoColor.Wild;
            }
            // Only allow WildDrawFour if no other playable cards
            if (card.Value == UnoValue.WildDrawFour)
            {
                return !PlayerHand.Any(c => c != card && c.Color != UnoColor.Wild && (c.Color == TopCard.Color || c.Value == TopCard.Value));
            }
            return card.Color == TopCard.Color || card.Value == TopCard.Value || card.Color == UnoColor.Wild;
        }
        public UnoColor? PendingWildColor { get; set; } = null;
        public bool AwaitingWildColor { get; set; } = false;
        public void ComputerMove()
        {
            if (!GameStarted) return;
            if (ComputerHand.Count == 0)
            {
                Message = "Computer wins!";
                GameStarted = false;
                return;
            }
            var playable = ComputerHand.FirstOrDefault(CanPlay);
            if (playable != null)
            {
                ComputerHand.Remove(playable);
                DiscardPile.Push(playable);
                if (ComputerHand.Count == 0)
                {
                    Message = "Computer wins!";
                    GameStarted = false;
                    return;
                }
                if (playable.Color == UnoColor.Wild)
                {
                    var color = GetMostCommonColor(ComputerHand);
                    PendingWildColor = color;
                    activeWildColor = color;
                    Message += $" Computer played {playable} and chose {color}.";
                    if (playable.Value == UnoValue.WildDrawFour)
                    {
                        Message += " You must draw 4 cards.";
                        PlayerDrawCards(4);
                        PlayerTurn = true;
                        // Check win after forced draw
                        if (ComputerHand.Count == 0)
                        {
                            Message = "Computer wins!";
                            GameStarted = false;
                        }
                        return;
                    }
                }
                else
                {
                    PendingWildColor = null;
                    activeWildColor = null;
                    Message += $" Computer played {playable}.";
                }
                // Handle Skip or Reverse card for computer
                if (playable.Value == UnoValue.Skip || playable.Value == UnoValue.Reverse)
                {
                    Message += playable.Value == UnoValue.Skip ? " Your turn is skipped." : " Reverse acts as Skip in a 2-player game.";
                    // In a two-player game, skip/reverse means computer gets another turn
                    PlayerTurn = false;
                    // Check win after skip/reverse
                    if (ComputerHand.Count == 0)
                    {
                        Message = "Computer wins!";
                        GameStarted = false;
                        return;
                    }
                    ComputerMove();
                    return;
                }
                // Handle Draw Two card for computer
                if (playable.Value == UnoValue.DrawTwo)
                {
                    Message += " You must draw 2 cards.";
                    PlayerDrawCards(2);
                    PlayerTurn = true;
                    // Check win after forced draw
                    if (ComputerHand.Count == 0)
                    {
                        Message = "Computer wins!";
                        GameStarted = false;
                    }
                    return;
                }
                PlayerTurn = true;
                // Check win at end of turn
                if (ComputerHand.Count == 0)
                {
                    Message = "Computer wins!";
                    GameStarted = false;
                }
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
                    if (ComputerHand.Count == 0)
                    {
                        Message = "Computer wins!";
                        GameStarted = false;
                        return;
                    }
                    Message += $" Computer played {drawnCard}.";
                    if (drawnCard.Color == UnoColor.Wild)
                    {
                        var color = GetMostCommonColor(ComputerHand);
                        PendingWildColor = color;
                        activeWildColor = color;
                        Message += $" Computer chose {color}.";
                        if (drawnCard.Value == UnoValue.WildDrawFour)
                        {
                            Message += " You must draw 4 cards.";
                            PlayerDrawCards(4);
                            PlayerTurn = true;
                            // Check win after forced draw
                            if (ComputerHand.Count == 0)
                            {
                                Message = "Computer wins!";
                                GameStarted = false;
                            }
                            return;
                        }
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
                        PlayerTurn = false;
                        // Check win after skip
                        if (ComputerHand.Count == 0)
                        {
                            Message = "Computer wins!";
                            GameStarted = false;
                            return;
                        }
                        ComputerMove();
                        return;
                    }
                    // Handle Draw Two card for computer
                    if (drawnCard.Value == UnoValue.DrawTwo)
                    {
                        Message += " You must draw 2 cards.";
                        PlayerDrawCards(2);
                        PlayerTurn = true;
                        // Check win after forced draw
                        if (ComputerHand.Count == 0)
                        {
                            Message = "Computer wins!";
                            GameStarted = false;
                        }
                        return;
                    }
                    PlayerTurn = true;
                    // Check win at end of turn
                    if (ComputerHand.Count == 0)
                    {
                        Message = "Computer wins!";
                        GameStarted = false;
                    }
                    return;
                }
            }
            PlayerTurn = true;
            // Check win at end of turn if no playable card
            if (ComputerHand.Count == 0)
            {
                Message = "Computer wins!";
                GameStarted = false;
            }
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
