namespace VKProxy.HttpRoutingStatement;

public class TokenParserContext
{
    private string data;
    private int index;
    public bool HasNext { get; private set; }
    public int Index { get => index; }
    public string Data { get => data; }

    public int Line { get; private set; }

    public int Column { get; private set; }

    public void NewLine()
    {
        Line += 1;
        Column = 1;
    }

    public void MoveColumn()
    {
        Column += 1;
    }

    public TokenParserContext(string sql)
    {
        this.data = sql;
        this.HasNext = data.Length > 0;
    }

    public bool TryNext(out char character)
    {
        if (HasNext)
        {
            index++;
            HasNext = index < data.Length;
            if (TryPeek(out character))
            {
                if (character == Symbols.NewLine)
                {
                    NewLine();
                }
                else
                {
                    MoveColumn();
                }
                return true;
            }
        }
        else
        {
            character = char.MaxValue;
        }
        return false;
    }

    public bool TryPeek(out char character)
    {
        if (index < data.Length)
        {
            character = Peek();
            return true;
        }
        else
        {
            character = char.MaxValue;
            return false;
        }
    }

    public bool TryPeek(int index, out char character)
    {
        if (index >= 0 && index < data.Length)
        {
            character = data[index];
            return true;
        }
        else
        {
            character = char.MaxValue;
            return false;
        }
    }

    public bool TryPeek(int index, int count, out ReadOnlySpan<char> character)
    {
        if (index >= 0 && index < data.Length)
        {
            character = data.AsSpan(index, count);
            return true;
        }
        else
        {
            character = ReadOnlySpan<char>.Empty;
            return false;
        }
    }

    public char Peek()
    {
        return data[index];
    }

    public string GetSomeChars()
    {
        return GetSomeChars(index);
    }

    public string GetSomeChars(int index)
    {
        if (index >= 0 && index < data.Length)
        {
            return data.Substring(index, Math.Min(50, data.Length - index));
        }
        else
        {
            return string.Empty;
        }
    }

    public void Reset(Token token)
    {
        Line = token.StartLine;
        Column = token.StartColumn;
        index = token.StartIndex;
        HasNext = index < data.Length;
    }
}