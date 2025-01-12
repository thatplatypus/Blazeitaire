using System.Collections.Immutable;
using Solitaire.Components;

namespace Solitaire.Game;

public class GameAction
{
    public Move Move { get; set; }
    
    public ImmutableList<List<Card>> TableauState { get;  set; }
    public ImmutableList<Card> StockState { get;  set; }
    public ImmutableList<Card> WasteState { get;  set; }
    public ImmutableList<List<Card>> FoundationsState { get; set; } 
}