namespace Solitaire.Game;

public class Move
{
    public MoveType Type { get; }
    public Card Card { get; }
    public int SourcePileIndex { get; }
    public int TargetPileIndex { get; }

    public Move(MoveType type, Card card, int source, int target)
    {
        Type = type;
        Card = card;
        SourcePileIndex = source;
        TargetPileIndex = target;
    }
}