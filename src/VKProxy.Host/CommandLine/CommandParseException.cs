namespace VKProxy.CommandLine;

public class CommandParseException : Exception
{
    public CommandParseException(string message) : base(message)
    {
    }
}
