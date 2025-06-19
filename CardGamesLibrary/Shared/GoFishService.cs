using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGamesLibrary.Shared
{
    public class GoFishService
    {
        public Player Human { get; private set; } = new() { Name = "You" };
        public Player Computer { get; private set; } = new() { Name = "Computer" };
        public Deck Deck { get; private set; } = new();
        public Player? CurrentPlayer { get; private set; }
        public string Message { get; private set; } = "";
        public bool GameOver { get; private set; } = false;
        public event Action<Rank>? ComputerAsked;

        public void StartNewGame()
        {
            Deck = new Deck();
            Deck.Shuffle();
            Human.Hand.Clear();
            Computer.Hand.Clear();
            Human.Books.Clear();
            Computer.Books.Clear();
            for (int i = 0; i < 7; i++)
            {
                Human.Hand.Add(Deck.Draw()!);
                Computer.Hand.Add(Deck.Draw()!);
            }
            CurrentPlayer = Human;
            Message = "Game started! Your turn.";
            GameOver = false;
        }

        public void RequestCard(Rank requestedRank)
        {
            if (GameOver) return;
            if (CurrentPlayer == Human)
            {
                var matches = Computer.Hand.Where(c => c.Rank == requestedRank).ToList();
                if (matches.Count > 0)
                {
                    foreach (var card in matches)
                    {
                        Human.Hand.Add(card);
                        Computer.Hand.Remove(card);
                    }
                    Message = $"You got {matches.Count} {requestedRank}(s) from Computer! Go again.";
                    CheckForBooks(Human);
                    if (Human.Hand.Count == 0 && Deck.Count > 0) Human.Hand.Add(Deck.Draw()!);
                    if (IsGameOver()) return;
                }
                else
                {
                    var drawn = Deck.Draw();
                    if (drawn != null)
                    {
                        Human.Hand.Add(drawn);
                        Message = $"Go Fish! You drew a {drawn}.";
                        CheckForBooks(Human);
                        if (drawn.Rank == requestedRank)
                        {
                            Message += " You drew what you asked for! Go again.";
                            if (IsGameOver()) return;
                            return;
                        }
                    }
                    else
                    {
                        Message = "Go Fish! Deck is empty.";
                    }
                    CurrentPlayer = Computer;
                    if (IsGameOver()) return;
                    ComputerTurn();
                }
            }
        }

        public void ComputerTurn()
        {
            if (GameOver) return;
            if (Computer.Hand.Count == 0 && Deck.Count > 0) Computer.Hand.Add(Deck.Draw()!);
            if (Computer.Hand.Count == 0) { GameOver = true; Message = GetWinnerMessage(); return; }
            var random = new Random();
            var rank = Computer.Hand[random.Next(Computer.Hand.Count)].Rank;
            ComputerAsked?.Invoke(rank);
            var matches = Human.Hand.Where(c => c.Rank == rank).ToList();
            if (matches.Count > 0)
            {
                foreach (var card in matches)
                {
                    Computer.Hand.Add(card);
                    Human.Hand.Remove(card);
                }
                Message = $"Computer asked for {rank} and got {matches.Count}! Computer goes again.";
                CheckForBooks(Computer);
                if (IsGameOver()) return;
                ComputerTurn();
            }
            else
            {
                var drawn = Deck.Draw();
                if (drawn != null)
                {
                    Computer.Hand.Add(drawn);
                    Message = $"Computer asked for {rank}. Go Fish!";
                    CheckForBooks(Computer);
                    if (drawn.Rank == rank)
                    {
                        Message += " Computer drew what it asked for and goes again.";
                        if (IsGameOver()) return;
                        ComputerTurn();
                        return;
                    }
                }
                else
                {
                    Message = $"Computer asked for {rank}. Go Fish! Deck is empty.";
                }
                CurrentPlayer = Human;
                if (IsGameOver()) return;
            }
        }

        private void CheckForBooks(Player player)
        {
            var grouped = player.Hand.GroupBy(c => c.Rank).Where(g => g.Count() == 4).ToList();
            foreach (var group in grouped)
            {
                player.Books.Add(group.Key);
                player.Hand.RemoveAll(c => c.Rank == group.Key);
                Message += $" {player.Name} completed a book of {group.Key}!";
            }
        }

        private bool IsGameOver()
        {
            if (Human.Hand.Count == 0 && Computer.Hand.Count == 0 && Deck.Count == 0)
            {
                GameOver = true;
                Message = GetWinnerMessage();
                return true;
            }
            return false;
        }

        private string GetWinnerMessage()
        {
            if (Human.Books.Count > Computer.Books.Count) return $"Game over! You win {Human.Books.Count} to {Computer.Books.Count}.";
            if (Human.Books.Count < Computer.Books.Count) return $"Game over! Computer wins {Computer.Books.Count} to {Human.Books.Count}.";
            return $"Game over! It's a tie: {Human.Books.Count} books each.";
        }
    }
}
