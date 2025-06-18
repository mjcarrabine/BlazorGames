using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CardGamesLibrary.Shared
{
    public class SolitaireGameService
    {
        public Deck? Deck { get; set; }
        public List<List<(Card card, bool faceUp)>> Tableau { get; set; } = new();
        public List<List<Card>> Foundations { get; set; } = new();
        public List<Card> Stock { get; set; } = new();
        public List<Card> Waste { get; set; } = new();
        public bool GameStarted { get; set; } = false;
        public int TableauPiles { get; set; } = 7;
        public bool IsWin { get; set; } = false;

        public event Action? OnStateChanged;

        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        private Stack<string> undoStack = new();
        private Stack<string> redoStack = new();

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        private string SerializeState()
        {
            // Only serialize the game state, not events or methods
            return JsonSerializer.Serialize(new SerializableState(this));
        }
        private void RestoreState(string json)
        {
            var state = JsonSerializer.Deserialize<SerializableState>(json);
            if (state != null)
            {
                state.ApplyTo(this);
            }
        }

        private void SaveStateForUndo()
        {
            undoStack.Push(SerializeState());
            redoStack.Clear();
        }
        private void SaveStateForRedo()
        {
            redoStack.Push(SerializeState());
        }
        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                SaveStateForRedo();
                RestoreState(undoStack.Pop());
                MoveCount = Math.Max(0, MoveCount - 1);
                // Optionally, you could also track score changes for undo/redo, but for now, just restore state
                NotifyStateChanged();
            }
        }
        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                var redoState = redoStack.Pop();
                undoStack.Push(SerializeState()); // Save current state for undo
                RestoreState(redoState);
                MoveCount++;
                // Optionally, you could also track score changes for undo/redo, but for now, just restore state
                NotifyStateChanged();
            }
        }

        public int MoveCount { get; set; } = 0;
        public int ElapsedSeconds { get; set; } = 0;
        private System.Timers.Timer? gameTimer;
        private DateTime? timerStart;

        public void StartTimer()
        {
            if (gameTimer == null)
            {
                gameTimer = new System.Timers.Timer(1000);
                gameTimer.Elapsed += (s, e) =>
                {
                    ElapsedSeconds++;
                    NotifyStateChanged();
                };
                gameTimer.AutoReset = true;
            }
            ElapsedSeconds = 0;
            timerStart = DateTime.Now;
            gameTimer.Start();
        }
        public void StopTimer()
        {
            gameTimer?.Stop();
        }
        public void ResetTimer()
        {
            ElapsedSeconds = 0;
            timerStart = null;
            gameTimer?.Stop();
        }

        private void IncrementMoveCount()
        {
            MoveCount++;
        }

        public int Score { get; set; } = 0;

        private void IncrementScore(int delta)
        {
            Score += delta;
        }
        private void DecrementScore(int delta)
        {
            Score = Math.Max(0, Score - delta);
        }

        // --- Customizable Rules ---
        public int DrawCount { get; set; } = 1; // 1 or 3
        public int MaxStockRecycles { get; set; } = -1; // -1 = unlimited, 0 = no recycles, 1 = one recycle, etc.
        public int StockRecycleCount { get; set; } = 0;

        public void SetRules(int drawCount, int maxStockRecycles)
        {
            DrawCount = drawCount;
            MaxStockRecycles = maxStockRecycles;
        }

        public void StartGame()
        {
            Deck = new Deck();
            Deck.Shuffle();
            Tableau.Clear();
            Foundations.Clear();
            Stock.Clear();
            Waste.Clear();
            StockRecycleCount = 0;
            // Deal cards to 7 tableau piles (Klondike style)
            for (int i = 0; i < TableauPiles; i++)
            {
                Tableau.Add(new List<(Card, bool)>());
                for (int j = 0; j <= i; j++)
                {
                    var card = Deck.Draw();
                    bool faceUp = (j == i); // Only the top card is face up
                    Tableau[i].Add((card, faceUp));
                }
            }
            // Create 4 empty foundation piles
            for (int i = 0; i < 4; i++)
                Foundations.Add(new List<Card>());
            // Remaining cards go to stock (face down)
            while (Deck != null && Deck.Count > 0)
            {
                Stock.Add(Deck.Draw());
            }
            GameStarted = true;
            IsWin = false;
            MoveCount = 0;
            Score = 0;
            ResetTimer();
            StartTimer();
            NotifyStateChanged();
        }

        // In Solitaire, Ace should be low (1), not high. Fix CanMoveToFoundation and CanMoveWasteToFoundation to use Ace as 1.
        public bool CanMoveToFoundation(int pileIndex, int foundationIndex)
        {
            if (Tableau.Count == 0 || pileIndex < 0 || pileIndex >= Tableau.Count)
                return false;
            if (Tableau[pileIndex].Count == 0)
                return false;
            var (card, faceUp) = Tableau[pileIndex].Last();
            if (!faceUp)
                return false;
            var foundation = Foundations[foundationIndex];
            if (foundation.Count == 0)
                return card.Rank == Rank.Ace;
            var last = foundation.Last();
            // Ace is low: 2 can be placed on Ace
            return card.Suit == last.Suit &&
                ((last.Rank == Rank.Ace && card.Rank == Rank.Two) || (int)card.Rank == (int)last.Rank + 1);
        }

        public void MoveToFoundation(int pileIndex, int foundationIndex)
        {
            SaveStateForUndo();
            if (Tableau.Count == 0 || pileIndex < 0 || pileIndex >= Tableau.Count)
                return;
            if (foundationIndex < 0 || foundationIndex >= Foundations.Count)
                return;
            if (CanMoveToFoundation(pileIndex, foundationIndex))
            {
                var (card, _) = Tableau[pileIndex].Last();
                Foundations[foundationIndex].Add(card);
                Tableau[pileIndex].RemoveAt(Tableau[pileIndex].Count - 1);
                // If there are cards left, flip the new top card face up
                if (Tableau[pileIndex].Count > 0)
                {
                    var (topCard, _) = Tableau[pileIndex].Last();
                    Tableau[pileIndex][Tableau[pileIndex].Count - 1] = (topCard, true);
                }
                // Win detection: all tableau piles empty and all foundations have 13 cards
                if (Tableau.All(p => p.Count == 0) && Foundations.All(f => f.Count == 13))
                {
                    IsWin = true;
                    StopTimer();
                }
                IncrementScore(10); // +10 for moving to foundation
                IncrementMoveCount();
                NotifyStateChanged();
            }
        }

        public bool CanMoveTableauToTableau(int fromPile, int cardIndex, int toPile)
        {
            if (fromPile < 0 || fromPile >= Tableau.Count || toPile < 0 || toPile >= Tableau.Count)
                return false;
            if (fromPile == toPile)
                return false;
            if (cardIndex < 0 || cardIndex >= Tableau[fromPile].Count)
                return false;
            var (movingCard, faceUp) = Tableau[fromPile][cardIndex];
            if (!faceUp)
                return false;
            // Only allow moving a sequence of face-up cards
            for (int i = cardIndex; i < Tableau[fromPile].Count; i++)
                if (!Tableau[fromPile][i].faceUp) return false;
            // If destination is empty, only Kings can be moved
            if (Tableau[toPile].Count == 0)
                return movingCard.Rank == Rank.King;
            var (destCard, destFaceUp) = Tableau[toPile].Last();
            // Must be opposite color and one rank lower
            bool isOppositeColor = (IsRed(movingCard.Suit) != IsRed(destCard.Suit));
            bool isOneLower = (int)movingCard.Rank == (int)destCard.Rank - 1;
            return destFaceUp && isOppositeColor && isOneLower;
        }

        public void MoveTableauToTableau(int fromPile, int cardIndex, int toPile)
        {
            SaveStateForUndo();
            if (!CanMoveTableauToTableau(fromPile, cardIndex, toPile))
                return;
            var movingCards = Tableau[fromPile].GetRange(cardIndex, Tableau[fromPile].Count - cardIndex);
            Tableau[toPile].AddRange(movingCards);
            Tableau[fromPile].RemoveRange(cardIndex, Tableau[fromPile].Count - cardIndex);
            // Flip new top card if needed
            if (Tableau[fromPile].Count > 0 && !Tableau[fromPile].Last().faceUp)
            {
                var (topCard, _) = Tableau[fromPile].Last();
                Tableau[fromPile][Tableau[fromPile].Count - 1] = (topCard, true);
                IncrementScore(5); // +5 for flipping a tableau card
            }
            IncrementMoveCount();
            NotifyStateChanged();
        }

        public bool CanMoveWasteToTableau(int tableauIndex)
        {
            if (Waste.Count == 0 || tableauIndex < 0 || tableauIndex >= Tableau.Count)
                return false;
            var card = Waste.Last();
            // If destination is empty, only Kings can be moved
            if (Tableau[tableauIndex].Count == 0)
                return card.Rank == Rank.King;
            var (destCard, destFaceUp) = Tableau[tableauIndex].Last();
            bool isOppositeColor = (IsRed(card.Suit) != IsRed(destCard.Suit));
            bool isOneLower = (int)card.Rank == (int)destCard.Rank - 1;
            return destFaceUp && isOppositeColor && isOneLower;
        }

        public void MoveWasteToTableau(int tableauIndex)
        {
            SaveStateForUndo();
            if (!CanMoveWasteToTableau(tableauIndex))
                return;
            var card = Waste.Last();
            Waste.RemoveAt(Waste.Count - 1);
            Tableau[tableauIndex].Add((card, true));
            IncrementScore(5); // +5 for moving waste to tableau
            IncrementMoveCount();
            NotifyStateChanged();
        }

        public bool CanMoveWasteToFoundation(int foundationIndex)
        {
            if (Waste.Count == 0 || foundationIndex < 0 || foundationIndex >= Foundations.Count)
                return false;
            var card = Waste.Last();
            var foundation = Foundations[foundationIndex];
            if (foundation.Count == 0)
                return card.Rank == Rank.Ace;
            var last = foundation.Last();
            // Ace is low: 2 can be placed on Ace
            return card.Suit == last.Suit &&
                ((last.Rank == Rank.Ace && card.Rank == Rank.Two) || (int)card.Rank == (int)last.Rank + 1);
        }

        public void MoveWasteToFoundation(int foundationIndex)
        {
            SaveStateForUndo();
            if (!CanMoveWasteToFoundation(foundationIndex))
                return;
            var card = Waste.Last();
            Waste.RemoveAt(Waste.Count - 1);
            Foundations[foundationIndex].Add(card);
            // Win detection: all tableau piles empty and all foundations have 13 cards
            if (Tableau.All(p => p.Count == 0) && Foundations.All(f => f.Count == 13))
            {
                IsWin = true;
                StopTimer();
            }
            IncrementScore(10); // +10 for moving waste to foundation
            IncrementMoveCount();
            NotifyStateChanged();
        }

        public void NewGame()
        {
            StartGame();
            NotifyStateChanged();
        }

        public void DrawFromStock()
        {
            SaveStateForUndo();
            if (Stock.Count > 0)
            {
                int draw = Math.Min(DrawCount, Stock.Count);
                for (int i = 0; i < draw; i++)
                {
                    var card = Stock.Last();
                    Stock.RemoveAt(Stock.Count - 1);
                    Waste.Add(card);
                }
                DecrementScore(1); // -1 for drawing from stock
                IncrementMoveCount();
                NotifyStateChanged();
            }
        }

        public void ResetStockFromWaste()
        {
            SaveStateForUndo();
            if (Stock.Count == 0 && Waste.Count > 0)
            {
                if (MaxStockRecycles >= 0 && StockRecycleCount >= MaxStockRecycles)
                    return; // No more recycles allowed
                Waste.Reverse();
                Stock.AddRange(Waste);
                Waste.Clear();
                StockRecycleCount++;
                DecrementScore(5); // -5 for recycling waste
                IncrementMoveCount();
                NotifyStateChanged();
            }
        }

        // Returns true if there is at least one legal move available in the current game state
        public bool HasAnyLegalMove()
        {
            // 1. Can draw from stock?
            if (Stock.Count > 0)
                return true;

            // 2. Can reset stock from waste?
            if (Stock.Count == 0 && Waste.Count > 0)
                return true;

            // 3. Can move waste to any foundation?
            for (int f = 0; f < Foundations.Count; f++)
                if (CanMoveWasteToFoundation(f))
                    return true;

            // 4. Can move waste to any tableau?
            for (int t = 0; t < Tableau.Count; t++)
                if (CanMoveWasteToTableau(t))
                    return true;

            // 5. Can move any tableau card to any foundation?
            for (int p = 0; p < Tableau.Count; p++)
                for (int f = 0; f < Foundations.Count; f++)
                    if (CanMoveToFoundation(p, f))
                        return true;

            // 6. Can move any face-up tableau sequence to another tableau pile?
            for (int from = 0; from < Tableau.Count; from++)
            {
                var pile = Tableau[from];
                for (int cardIdx = 0; cardIdx < pile.Count; cardIdx++)
                {
                    if (!pile[cardIdx].faceUp) continue;
                    for (int to = 0; to < Tableau.Count; to++)
                    {
                        if (from == to) continue;
                        if (CanMoveTableauToTableau(from, cardIdx, to))
                            return true;
                    }
                }
            }
            // No legal moves found
            return false;
        }

        private bool IsRed(Suit suit) => suit == Suit.Hearts || suit == Suit.Diamonds;

        public List<Card> GetWasteDisplay()
        {
            // Show up to DrawCount cards from the end of the waste pile (top of waste)
            int count = Math.Min(DrawCount, Waste.Count);
            if (count == 0) return new List<Card>();
            return Waste.Skip(Waste.Count - count).ToList();
        }
    }

    // Helper class for serializing only the game state
    public class SerializableState
    {
        public List<List<SerializableCard>>? Tableau { get; set; }
        public List<List<Card>>? Foundations { get; set; }
        public List<Card>? Stock { get; set; }
        public List<Card>? Waste { get; set; }
        public bool GameStarted { get; set; }
        public int TableauPiles { get; set; }
        public bool IsWin { get; set; }

        public SerializableState()
        {
            Tableau = new();
            Foundations = new();
            Stock = new();
            Waste = new();
        }
        public SerializableState(SolitaireGameService svc)
        {
            Tableau = svc.Tableau.Select(pile => pile.Select(c => new SerializableCard(c.card, c.faceUp)).ToList()).ToList();
            Foundations = svc.Foundations.Select(f => f.ToList()).ToList();
            Stock = svc.Stock.ToList();
            Waste = svc.Waste.ToList();
            GameStarted = svc.GameStarted;
            TableauPiles = svc.TableauPiles;
            IsWin = svc.IsWin;
        }
        public void ApplyTo(SolitaireGameService svc)
        {
            svc.Tableau = Tableau?.Select(pile => pile
                .Where(c => c.Card != null)
                .Select(c => (c.Card!, c.FaceUp)).ToList()).ToList() ?? new();
            svc.Foundations = Foundations?.Select(f => f.ToList()).ToList() ?? new();
            svc.Stock = Stock?.ToList() ?? new();
            svc.Waste = Waste?.ToList() ?? new();
            svc.GameStarted = GameStarted;
            svc.TableauPiles = TableauPiles;
            svc.IsWin = IsWin;
        }
    }

    public class SerializableCard
    {
        public Card? Card { get; set; }
        public bool FaceUp { get; set; }
        public SerializableCard() { }
        public SerializableCard(Card card, bool faceUp)
        {
            Card = card;
            FaceUp = faceUp;
        }
    }
}
