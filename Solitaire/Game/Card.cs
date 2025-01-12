namespace Solitaire.Game;

public class Card
{
    public Suit Suit { get; }
    public Rank Rank { get; }
    public bool IsFaceUp { get; set; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
        IsFaceUp = false;
    }
}
