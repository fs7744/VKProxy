namespace VKProxy.HttpRoutingStatement;

public class StringTokenParser : ITokenParser
{
    public bool TryTokenize(TokenParserContext context, out Token t)
    {
        if (context.TryPeek(out var c) && (c == Symbols.SingleQuote || c == Symbols.DoubleQuote))
        {
            t = Token.New(context);
            t.Type = TokenType.String;
            if (TryParseString(t))
            {
                return true;
            }
            t.Reset();
        }
        t = null;
        return false;
    }

    private bool TryParseString(Token t)
    {
        var context = t.Context;
        var p = context.Peek();
        while (context.TryNext(out var character))
        {
            if (character == Symbols.Backslash)
            {
                if (context.TryNext(out character))
                {
                    if (p == character)
                    {
                        continue;
                    }
                }
            }
            else if (p == character)
            {
                context.TryNext(out character);
                break;
            }
        }
        t.Count = context.Index - t.StartIndex;
        return t.Count > 0;
    }
}