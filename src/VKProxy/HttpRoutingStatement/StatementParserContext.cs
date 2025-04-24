namespace VKProxy.HttpRoutingStatement;

public class StatementParserContext : IEnumerator<Token>
{
    public StatementParserContext(Token[] tokens, Action<StatementParserContext, bool> parser)
    {
        Stack = new Stack<Statement>();
        Tokens = tokens;
        Parse = parser;
    }

    public Token[] Tokens { get; }
    public Action<StatementParserContext, bool> Parse { get; }
    public Stack<Statement> Stack { get; }

    public int Index { get; set; }

    public StatementState State { get; set; }

    public Token Current => Tokens[Index];

    object System.Collections.IEnumerator.Current => Current;

    public void Dispose()
    {
    }

    public bool HasToken()
    {
        return Index < Tokens.Length;
    }

    public bool MoveNext()
    {
        if (HasToken())
        {
            Index++;
            return HasToken();
        }
        return false;
    }

    public void Reset()
    {
    }
}