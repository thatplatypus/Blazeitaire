using System.Collections.Immutable;

namespace Solitaire.Game;

public static class ImmutableListExtensions
{
    public static Stack<T> ToStack<T>(this ImmutableList<T> list)
    {
        return new Stack<T>(list);
    }
}