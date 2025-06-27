using VKProxy.HttpRoutingStatement;

namespace VKProxy.TemplateStatement;

public class TemplateStatementTokenParser : ITokenParser
{
    public bool TryTokenize(TokenParserContext context, out Token t)
    {
        if (context.TryPeek(out var c) && c != Symbols.CurlyBracketOpen && c != Symbols.CurlyBracketClose)
        {
            t = Token.New(context);
            t.Type = TokenType.Word;
            context.TryNext(out var _);
            while (context.TryPeek(out c) && c != Symbols.CurlyBracketOpen && c != Symbols.CurlyBracketClose)
            {
                context.TryNext(out var _);
            }
            t.Count = context.Index - t.StartIndex;
            return t.Count > 0;
        }

        t = null;
        return false;
    }
}