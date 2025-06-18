namespace CardGamesLibrary.Shared
{
    public class WarStep
    {
        public List<Card?> Player1Cards { get; set; } = new();
        public List<bool> Player1FaceUp { get; set; } = new();
        public List<Card?> Player2Cards { get; set; } = new();
        public List<bool> Player2FaceUp { get; set; } = new();
    }
}