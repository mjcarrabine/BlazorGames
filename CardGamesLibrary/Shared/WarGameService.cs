using System;
using System.Collections.Generic;

namespace CardGamesLibrary.Shared
{
    public class WarGameService
    {
        public List<Card> Player1Pile { get; private set; } = new();
        public List<Card> Player2Pile { get; private set; } = new();
        public List<Card> Player1WonPile { get; private set; } = new();
        public List<Card> Player2WonPile { get; private set; } = new();
        public Card? Player1Card { get; private set; }
        public Card? Player2Card { get; private set; }
        public Card? PreviousPlayer1Card { get; private set; }
        public Card? PreviousPlayer2Card { get; private set; }
        public int Player1Score { get; private set; }
        public int Player2Score { get; private set; }
        public string? LastResult { get; private set; }
        public bool GameStarted { get; private set; }
        public bool RoundInProgress { get; private set; }
        public string Player1Name { get; set; } = "Player 1";
        public string Player2Name { get; set; } = "Player 2";

        // War state
        public List<WarStep> WarSteps { get; private set; } = new();
        public bool IsWarOngoing { get; private set; } = false;
        public int CurrentWarStepIndex { get; private set; } = -1;

        public void StartGame()
        {
            var deck = new Deck();
            deck.Shuffle();
            Player1Pile = new List<Card>();
            Player2Pile = new List<Card>();
            Player1WonPile = new List<Card>();
            Player2WonPile = new List<Card>();
            WarSteps = new List<WarStep>();
            IsWarOngoing = false;
            for (int i = 0; i < 52; i++)
            {
                //if (i % 2 == 0)
                if (i < 26)
                    Player1Pile.Add(deck.Deal()!);
                else
                    Player2Pile.Add(deck.Deal()!);
            }
            Player1Score = 0;
            Player2Score = 0;
            Player1Card = null;
            Player2Card = null;
            PreviousPlayer1Card = null;
            PreviousPlayer2Card = null;
            LastResult = null;
            GameStarted = true;
            RoundInProgress = false;
        }

        public void PlayRound()
        {
            // If a player's pile is empty, move their won pile to their pile (no shuffle)
            if (Player1Pile.Count == 0 && Player1WonPile.Count > 0)
            {
                Player1Pile.AddRange(Player1WonPile);
                Player1WonPile.Clear();
            }
            if (Player2Pile.Count == 0 && Player2WonPile.Count > 0)
            {
                Player2Pile.AddRange(Player2WonPile);
                Player2WonPile.Clear();
            }

            // If either pile is still empty, the game is over
            if (IsGameOver())
            {
                Player1Card = null;
                Player2Card = null;
                return;
            }

            //// Clear war steps if not in war and war steps exist
            //if (!IsWarOngoing && WarSteps.Count > 0)
            //{
            //    WarSteps.Clear();
            //    CurrentWarStepIndex = -1;
            //}

            if (!GameStarted || IsGameOver()) return;

            if (IsWarOngoing)
            {
                // Add one war step per call
                PlayNextWarStep();
                return;
            }

            // Move previous face-up cards to the won pile
            if (Player1Card != null && Player2Card != null)
            {
                if ((int)Player1Card.Rank > (int)Player2Card.Rank)
                {
                    Player1WonPile.Add(Player1Card);
                    Player1WonPile.Add(Player2Card);
                    CollectWarSteps(Player1WonPile);
                }
                else if ((int)Player2Card.Rank > (int)Player1Card.Rank)
                {
                    Player2WonPile.Add(Player1Card);
                    Player2WonPile.Add(Player2Card);
                    CollectWarSteps(Player2WonPile);
                }
                //else
                //{
                //    // Start war
                //    IsWarOngoing = true;
                //    WarSteps = new List<WarStep>();
                //    CurrentWarStepIndex = 0;
                //    LastResult = "War! Each player places 3 cards face down and 1 face up.";

                //    // Add the tied cards as the first war step (face up)
                //    var tiedStep = new WarStep();
                //    tiedStep.Player1Cards.Add(Player1Card);
                //    tiedStep.Player1FaceUp.Add(true);
                //    tiedStep.Player2Cards.Add(Player2Card);
                //    tiedStep.Player2FaceUp.Add(true);
                //    WarSteps.Add(tiedStep);

                //    // Do NOT set Player1Card/Player2Card to null here; let the UI show them as the first war step
                //    // They will be updated as the war progresses
                //    return;
                //}
            }

            // Store previous face-up cards for display
            PreviousPlayer1Card = Player1Card;
            PreviousPlayer2Card = Player2Card;

            // Draw new face-up cards
            Player1Card = Player1Pile[0];
            Player1Pile.RemoveAt(0);
            Player2Card = Player2Pile[0];
            Player2Pile.RemoveAt(0);

            if ((int)Player1Card.Rank > (int)Player2Card.Rank)
            {
                Player1Score++;
                LastResult = $"{Player1Name} wins the round!";
            }
            else if ((int)Player2Card.Rank > (int)Player1Card.Rank)
            {
                Player2Score++;
                LastResult = $"{Player2Name} wins the round!";
            }
            else
            {
                // Start war
                IsWarOngoing = true;
                WarSteps = new List<WarStep>();
                CurrentWarStepIndex = 0;
                LastResult = "War! Each player places 3 cards face down and 1 face up.";

                // Add the tied cards as the first war step (face up)
                var tiedStep = new WarStep();
                tiedStep.Player1Cards.Add(Player1Card);
                tiedStep.Player1FaceUp.Add(true);
                tiedStep.Player2Cards.Add(Player2Card);
                tiedStep.Player2FaceUp.Add(true);
                WarSteps.Add(tiedStep);

                // they have been added to the war step
                Player1Card = null;
                Player2Card = null;
                return;
            }

            RoundInProgress = false;
        }

        private void PlayNextWarStep()
        {
            CurrentWarStepIndex++;
            var step = new WarStep();
            // Add up to 3 face down cards per player
            if (CurrentWarStepIndex <= 3)
            {
                // Player 1
                if (Player1Pile.Count > 0)
                {
                    step.Player1Cards.Add(Player1Pile[0]);
                    step.Player1FaceUp.Add(false);
                    Player1Pile.RemoveAt(0);
                }
                else
                {
                    step.Player1Cards.Add(null);
                    step.Player1FaceUp.Add(false);
                }
                // Player 2
                if (Player2Pile.Count > 0)
                {
                    step.Player2Cards.Add(Player2Pile[0]);
                    step.Player2FaceUp.Add(false);
                    Player2Pile.RemoveAt(0);
                }
                else
                {
                    step.Player2Cards.Add(null);
                    step.Player2FaceUp.Add(false);
                }

                WarSteps.Add(step);
            }
            if (CurrentWarStepIndex == 4)
            {
                Player1Card = Player1Pile[0];
                Player1Pile.RemoveAt(0);
                Player2Card = Player2Pile[0];
                Player2Pile.RemoveAt(0);
            }
            //// Repeat for face up card
            //if (Player1Pile.Count > 0)
            //{
            //    step.Player1Cards.Add(Player1Pile[0]);
            //    step.Player1FaceUp.Add(true);
            //    Player1Card = Player1Pile[0];
            //    Player1Pile.RemoveAt(0);
            //}
            //else
            //{
            //    step.Player1Cards.Add(null);
            //    step.Player1FaceUp.Add(true);
            //    Player1Card = null;
            //}
            //if (Player2Pile.Count > 0)
            //{
            //    step.Player2Cards.Add(Player2Pile[0]);
            //    step.Player2FaceUp.Add(true);
            //    Player2Card = Player2Pile[0];
            //    Player2Pile.RemoveAt(0);
            //}
            //else
            //{
            //    step.Player2Cards.Add(null);
            //    step.Player2FaceUp.Add(true);
            //    Player2Card = null;
            //}

            if (CurrentWarStepIndex == 4)
            {
                IsWarOngoing = false;
            }

            //// If either player ran out of cards, or this is the last step, determine winner
            //if (Player1Card == null || Player2Card == null ||
            //    (Player1Card != null && Player2Card != null && (Player1Card.Rank != Player2Card.Rank)))
            //{
            //    // Determine winner, set IsWarOngoing = false, but do NOT clear WarSteps
            //    // (winner logic here)
            //    IsWarOngoing = false;
            //}
        }

        private List<Card> GetAllWarStepCards()
        {
            var all = new List<Card>();
            foreach (var step in WarSteps)
            {
                foreach (var c in step.Player1Cards)
                    if (c != null) all.Add(c);
                foreach (var c in step.Player2Cards)
                    if (c != null) all.Add(c);
            }
            return all;
        }

        private void CollectWarSteps(List<Card> winnerPile)
        {
            if (WarSteps.Count > 0)
            {
                winnerPile.AddRange(GetAllWarStepCards());
                WarSteps.Clear();
                IsWarOngoing = false;
            }
        }

        public int CardsRemaining => Player1Pile.Count + Player1WonPile.Count > 0 && Player2Pile.Count + Player2WonPile.Count > 0
            ? Math.Min(Player1Pile.Count + Player1WonPile.Count, Player2Pile.Count + Player2WonPile.Count)
            : 0;

        public int Player1Count => Player1Pile.Count + Player1WonPile.Count;
        public int Player2Count => Player2Pile.Count + Player2WonPile.Count;
        public string Status => LastResult ?? string.Empty;

        public bool IsGameOver()
        {
            return (Player1Pile.Count == 0 && Player1WonPile.Count == 0) ||
                   (Player2Pile.Count == 0 && Player2WonPile.Count == 0);
        }
    }
}