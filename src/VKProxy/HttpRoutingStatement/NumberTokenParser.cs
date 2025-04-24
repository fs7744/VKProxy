using System.Collections.Frozen;

namespace VKProxy.HttpRoutingStatement;

public class NumberTokenParser : ITokenParser
{
    private static readonly FrozenSet<char> chars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }.ToFrozenSet();

    public bool TryTokenize(TokenParserContext context, out Token t)
    {
        if (context.TryPeek(out var c))
        {
            t = Token.New(context);
            t.Type = TokenType.Number;
            if (TryParseNumber(t))
            {
                return true;
            }
            t.Reset();
        }
        t = null;
        return false;
    }

    private bool TryParseNumber(Token t)
    {
        var context = t.Context;
        var hasMinus = false;
        bool hasDot = false;
        bool hasNum = false;
        while (context.TryPeek(out var c))
        {
            if (chars.Contains(c))
            {
                hasNum = true;
            }
            else if (c == Symbols.Dot)
            {
                if (!hasNum) return false;
                if (hasDot)
                {
                    //break;
                    throw new ParserExecption($"Can't parse near by {context.GetSomeChars(t.StartIndex)} (Line:{t.StartLine},Col:{t.StartColumn})");
                }
                hasDot = true;
            }
            else if (c == Symbols.Minus)
            {
                if (hasMinus)
                {
                    break;
                }
                hasMinus = true;
            }
            else
            {
                break;
            }
            context.TryNext(out var _);
        }
        t.Count = context.Index - t.StartIndex;
        return hasMinus ? t.Count > 1 : t.Count > 0;
    }
}