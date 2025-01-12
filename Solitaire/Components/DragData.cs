namespace Solitaire.Components;

public class DragData
{
    public Game.Card Card { get; set; } = default!;
    public string SourcePile { get; set; } = "";
    public int SourceIndex { get; set; } 
    public int? CardIndex { get; set; } 

    public DragData(Game.Card card, string sourcePile, int sourceIndex, int? cardIndex = null)
    {
        Card = card;
        SourcePile = sourcePile;
        SourceIndex = sourceIndex;
        CardIndex = cardIndex;
    }
}