using System.Collections.Frozen;

namespace VKProxy.HttpRoutingStatement;

public class IngoreTokenParser : ITokenParser
{
    internal static readonly FrozenSet<char> chars = new char[] { Symbols.NewLine, Symbols.Whitespace, Symbols.CarriageReturn, Symbols.Tab, Symbols.EOF }.ToFrozenSet();

    public bool TryTokenize(TokenParserContext context, out Token t)
    {
        t = null;
        if (context.TryPeek(out var c) && chars.Contains(c))
        {
            context.TryNext(out c);
            return true;
        }
        return false;
    }
}