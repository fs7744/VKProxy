using VKProxy.HttpRoutingStatement;

namespace VKProxy.TemplateStatement;

public class TemplateStatementSignTokenParser : ITokenParser
{
    public bool TryTokenize(TokenParserContext context, out Token t)
    {
        if (context.TryPeek(out var c) && (c == Symbols.CurlyBracketOpen || c == Symbols.CurlyBracketClose))
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
            case Symbols.CurlyBracketOpen:
                context.TryNext(out var _);
                if (context.TryPeek(out var c) && c == Symbols.CurlyBracketOpen)
                {
                    context.TryNext(out var _);
                }
                break;

            case Symbols.CurlyBracketClose:
                context.TryNext(out var _);
                if (context.TryPeek(out c) && c == Symbols.CurlyBracketClose)
                {
                    context.TryNext(out var _);
                }
                break;

            default:
                break;
        }
        t.Count = context.Index - t.StartIndex;
        return t.Count > 0;
    }
}
