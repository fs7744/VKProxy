using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

[Serializable]
internal class AcmeRequestException : Exception
{
    public AcmeError Error { get; private set; }

    public AcmeRequestException()
    {
    }

    public AcmeRequestException(string? message) : base(message)
    {
    }

    public AcmeRequestException(string message, AcmeError error) : base($"{message} {error?.Detail}")
    {
        this.Error = error;
    }

    public AcmeRequestException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}