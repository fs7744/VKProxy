using System.Collections.Frozen;

namespace VKProxy.HttpRoutingStatement;

public class WordTokenParser : ITokenParser
{
    internal static readonly FrozenSet<char> chars = IngoreTokenParser.chars.Union(new char[]
    { Symbols.ParenClose, Symbols.ParenOpen, Symbols.Plus, Symbols.Divide, Symbols.Dot, Symbols.LessThan, Symbols.GreaterThan, Symbols.Equal,
            Symbols.Comma, Symbols.Semicolon, Symbols.Minus, Symbols.ExclamationMark, Symbols.Tilde,Symbols.Pipe, Symbols.Backtick, Symbols.Num, Symbols.Dollar,
            Symbols.Asterisk, Symbols.Caret,Symbols.QuestionMark, Symbols.Colon, Symbols.Percent, Symbols.SquareBracketClose, Symbols.SquareBracketOpen,
            Symbols.CurlyBracketClose, Symbols.CurlyBracketOpen, Symbols.SingleQuote, Symbols.DoubleQuote,
    }).ToFrozenSet();

    public bool TryTokenize(TokenParserContext context, out Token t)
    {
        if (context.TryPeek(out var c))
        {
            t = Token.New(context);
            t.Type = TokenType.Word;
            if (TryParseWord(t))
            {
                return true;
            }
            t.Reset();
        }
        t = null;
        return false;
    }

    private bool TryParseWord(Token t)
    {
        var context = t.Context;
        while (context.TryPeek(out var c) && !chars.Contains(c))
        {
            context.TryNext(out c);
        }
        t.Count = context.Index - t.StartIndex;
        var r = t.Count > 0;
        if (r)
        {
            var v = t.GetValue();
            if (v.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                t.Type = TokenType.True;
            }
            else if (v.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                t.Type = TokenType.False;
            }
            else if (v.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                t.Type = TokenType.Null;
            }
        }
        return r;
    }
}