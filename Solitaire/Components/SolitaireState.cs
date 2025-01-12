using System.Collections.Immutable;
using System.Text.Json;
using Solitaire.Game;

namespace Solitaire.Components;

public class SolitaireState
{
    public event Action StateChanged;

    public event Action GameWon;
    
    public void NotifyGameEnded() => GameWon?.Invoke();
    
    public void NotifyStateChanged() => StateChanged?.Invoke();
    
    public List<List<Card>> Tableau { get; private set; }
    public Stack<Card> Stock { get; private set; }
    public Stack<Card> Waste { get; private set; }
    public List<Stack<Card>> Foundations { get; private set; }

    public Stack<GameAction> UndoStack { get; private set; } = [];

    public bool CanUndo => UndoStack.Count > 0;
    public Stack<GameAction> RedoStack { get; private set; } = [];

    public bool CanRedo => RedoStack.Count > 0;

    public List<Card> Deck { get; private set; }
    
    public DragData? CurrentDragData { get; set; }

    public SolitaireState()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        Deck = GenerateDeck();
        ShuffleDeck();

        Tableau = Enumerable.Range(0, 7).Select(_ => new List<Card>()).ToList();
        Stock = new Stack<Card>();
        Waste = new Stack<Card>();
        Foundations = Enumerable.Range(0, 4).Select(_ => new Stack<Card>()).ToList();

        for (int i = 0; i < 7; i++)
        {
            for (int j = i; j < 7; j++)
            {
                Tableau[j].Add(Deck[^1]);
                Deck.RemoveAt(Deck.Count - 1);
            }
            Tableau[i].Last().IsFaceUp = true; 
        }

        foreach (var card in Deck)
            Stock.Push(card);

        Deck.Clear(); 
        NotifyStateChanged();
    }

    private List<Card> GenerateDeck()
    {
        var suits = Enum.GetValues<Suit>();
        var ranks = Enum.GetValues<Rank>();
        return suits.SelectMany(suit => ranks.Select(rank => new Card(suit, rank))).ToList();
    }

    private void ShuffleDeck()
    {
        Random rng = new();
        for (int i = Deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (Deck[i], Deck[j]) = (Deck[j], Deck[i]);
        }
    }
    
    
    public bool PerformMove(Move move)
    {
        Console.WriteLine($"Attempting move: {move.Type} from {move.SourcePileIndex} to {move.TargetPileIndex}");

        if (!IsValidMove(move))
        {
            Console.WriteLine("Move not valid: " + JsonSerializer.Serialize(move));
            return false;
        }

        UndoStack.Push(new GameAction
        {
            Move = move, 
            WasteState =  Waste.ToImmutableList(),
            FoundationsState = Foundations.Select(f => f.ToList()).ToImmutableList(),
            StockState =  Stock.ToImmutableList(),
            TableauState =  Tableau.Select(x => x.ToList()).ToImmutableList(),
        });
        RedoStack.Clear();
        
        Console.WriteLine("Validated move, executing...");

        switch (move.Type)
        {
            case MoveType.TableauToFoundation:
                if (move.SourcePileIndex >= 0)
                {
                    Foundations[move.TargetPileIndex].Push(move.Card);
                    Tableau[move.SourcePileIndex].Remove(move.Card);
                    FlipTopCard(Tableau[move.SourcePileIndex]);
                }
                else if (move.SourcePileIndex == -1) 
                {
                    Foundations[move.TargetPileIndex].Push(move.Card);
                    Waste.Pop();
                }
                break;

            case MoveType.TableauToTableau:
                var sourcePile = Tableau[move.SourcePileIndex];
                var cardsToMove = sourcePile.SkipWhile(c => c != move.Card).ToList();
                Tableau[move.SourcePileIndex].RemoveRange(sourcePile.Count - cardsToMove.Count, cardsToMove.Count);
                Tableau[move.TargetPileIndex].AddRange(cardsToMove);
                
                if(!Tableau[move.SourcePileIndex].Any(x=> x.IsFaceUp))
                    FlipTopCard(Tableau[move.SourcePileIndex]);
                
                break;

            case MoveType.StockToWaste:
                var c = Stock.Pop();
                c.IsFaceUp = true;
                Waste.Push(c);
                break;

            case MoveType.WasteToTableau:
                Tableau[move.TargetPileIndex].Add(Waste.Pop());
                break;

            case MoveType.WasteToFoundation:
                Foundations[move.TargetPileIndex].Push(Waste.Pop());
                break;
        }
        
        NotifyStateChanged();
        
        if (IsGameWon())
        {
            Console.WriteLine("Congratulations! You've won the game!");
            NotifyGameEnded();
        }

        return true;
    }
    private bool IsValidMove(Move move)
    {
        Console.WriteLine(move.Type);
        switch (move.Type)
        {
            case MoveType.TableauToFoundation:
                if (!Foundations[move.TargetPileIndex].TryPeek(out var topFoundationCard))
                {
                    return move.Card.Rank == Rank.Ace;
                }

                return topFoundationCard.Suit == move.Card.Suit && (int)move.Card.Rank == (int)topFoundationCard.Rank + 1;

            case MoveType.TableauToTableau:
                return Tableau[move.TargetPileIndex].LastOrDefault() is { } topTableauCard
                    ? topTableauCard.IsFaceUp && AreAlternatingColors(topTableauCard, move.Card) && 
                      (int)move.Card.Rank == (int)topTableauCard.Rank - 1
                    : move.Card.Rank == Rank.King;

            case MoveType.StockToWaste:
                return Stock.Any();

            case MoveType.WasteToTableau:
                return IsValidMove(new Move(MoveType.TableauToTableau, move.Card, -1, move.TargetPileIndex));

            case MoveType.WasteToFoundation:
                return IsValidMove(new Move(MoveType.TableauToFoundation, move.Card, -1, move.TargetPileIndex));

            default:
                return false;
        }
    }

    private bool AreAlternatingColors(Card card1, Card card2)
    {
        bool isRed1 = card1.Suit == Suit.Hearts || card1.Suit == Suit.Diamonds;
        bool isRed2 = card2.Suit == Suit.Hearts || card2.Suit == Suit.Diamonds;
        return isRed1 != isRed2;
    }
    public void Undo()
    {
        if (UndoStack.TryPop(out var lastAction))
        {
            RedoStack.Push(lastAction);
            Tableau = lastAction.TableauState.ToList();
            Stock = lastAction.StockState.ToStack();
            Waste = lastAction.WasteState.ToStack();
            Foundations = lastAction.FoundationsState.Select(f => new Stack<Card>(f)).ToList();
            NotifyStateChanged();
        }
    }

    public void Redo()
    {
        if (RedoStack.TryPop(out var nextAction))
        {
            PerformMove(nextAction.Move);
            UndoStack.Push(nextAction);
        }
    }
    private void FlipTopCard(List<Card> pile)
    {
        if (pile.Any() && !pile.Last().IsFaceUp)
        {
            pile.Last().IsFaceUp = true;
        }
    }
    public bool IsGameWon()
    {
        return Foundations.All(foundation => foundation.Count == 13);
    }
}