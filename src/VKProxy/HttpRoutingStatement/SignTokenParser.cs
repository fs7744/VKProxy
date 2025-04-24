namespace VKProxy.HttpRoutingStatement;

public class SignTokenParser : ITokenParser
{
    public bool TryTokenize(TokenParserContext context, out Token t)
    {
        if (context.TryPeek(out var c))
        {
            t = Token.New(context);
            t.Type = TokenType.Sign;
            if (TryParseSign(t))
            {
                return true;
            }
            t.Reset();
        }
        t = null;
        return false;
    }

    private bool TryParseSign(Token t)
    {
        var context = t.Context;
        switch (context.Peek())
        {
            case Symbols.LessThan:
            case Symbols.GreaterThan:
            case Symbols.ExclamationMark:
                if (context.TryNext(out var c) && c == Symbols.Equal)
                {
                    context.TryNext(out var _);
                }
                break;

            default:
                context.TryNext(out var _);
                break;
        }
        t.Count = context.Index - t.StartIndex;
        return t.Count > 0;
    }
}