namespace VKProxy.HttpRoutingStatement;

public class Token
{
    public TokenType Type { get; set; }
    public TokenParserContext Context { get; set; }
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public int StartIndex { get; set; }
    public int Count { get; set; }

    public void Reset()
    {
        Context.Reset(this);
    }

    public void End()
    {
        EndLine = Context.Line;
        EndColumn = Context.Column;
    }

    public static Token New(TokenParserContext context)
    {
        return new Token()
        {
            Context = context,
            StartLine = context.Line,
            StartColumn = context.Column,
            StartIndex = context.Index,
        };
    }

    public ReadOnlySpan<char> GetValue()
    {
        var r = Context.Data.AsSpan(StartIndex, Count);
        if (Type == TokenType.String)
        {
            return r[0] == Symbols.SingleQuote
                ? r[1..^1].ToString().Replace("\\'", "'")
                : r[1..^1].ToString().Replace("\\\"", "\"");
        }
        return r;
    }

    public override string ToString()
    {
        return $"{Enum.GetName<TokenType>(Type)},{GetValue()}";
    }
}