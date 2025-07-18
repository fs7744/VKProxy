using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

[Serializable]
public class AcmeException : Exception
{
    public AcmeError Error { get; private set; }

    public AcmeException()
    {
    }

    public AcmeException(string? message) : base(message)
    {
    }

    public AcmeException(string message, AcmeError error) : base($"{message} {error?.Detail}")
    {
        this.Error = error;
    }

    public AcmeException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}