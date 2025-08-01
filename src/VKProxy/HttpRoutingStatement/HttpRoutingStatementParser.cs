using Microsoft.AspNetCore.Http;

namespace VKProxy.HttpRoutingStatement;

public static partial class HttpRoutingStatementParser
{
    private static readonly IStatementParser[] statementParsers;
    private static readonly ITokenParser[] tokenParsers;

    static HttpRoutingStatementParser()
    {
        statementParsers = new IStatementParser[] { new OperaterStatementParser(), new ValueStatementParser() };
        tokenParsers = new ITokenParser[] { new IngoreTokenParser(), new StringTokenParser(), new NumberTokenParser(), new WordTokenParser(), new SignTokenParser() };
    }

    public static Stack<Statement> ParseStatements(string statement)
    {
        var context = new StatementParserContext(Tokenize(statement).ToArray(), ParseStatements);
        ParseStatements(context, false);
        return context.Stack;
    }

    private static void ParseStatements(StatementParserContext context, bool doOnce)
    {
        while (context.HasToken())
        {
            bool matched = false;
            foreach (var parser in statementParsers)
            {
                if (parser.TryParse(context))
                {
                    matched = true;
                    break;
                }
            }
            if (doOnce) break;
            if (!matched && context.HasToken())
            {
                var c = context.Current;
                throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
            }
        }
    }

    public static IEnumerable<Token> Tokenize(string statement)
    {
        var context = new TokenParserContext(statement);
        while (context.TryPeek(out var character))
        {
            bool matched = false;
            foreach (var parser in tokenParsers)
            {
                if (parser.TryTokenize(context, out var t))
                {
                    matched = true;
                    if (t != null)
                        yield return t;
                    break;
                }
            }
            if (!matched)
                throw new ParserExecption($"Can't parse near by {context.GetSomeChars()} (Line:{context.Line},Col:{context.Column})");
            if (!context.HasNext) { break; }
        }
    }
}