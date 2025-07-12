namespace VKProxy.CommandLine;

public class DoCommandException : Exception
{
    public DoCommandException(string cmd) : base(cmd)
    {
    }
}
