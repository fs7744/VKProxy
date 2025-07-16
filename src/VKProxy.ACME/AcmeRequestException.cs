using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

[Serializable]
internal class AcmeRequestException : Exception
{
    private string v;
    private AcmeError error;

    public AcmeRequestException()
    {
    }

    public AcmeRequestException(string? message) : base(message)
    {
    }

    public AcmeRequestException(string v, AcmeError error)
    {
        this.v = v;
        this.error = error;
    }

    public AcmeRequestException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}