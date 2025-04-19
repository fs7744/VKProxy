using VKProxy.Middlewares.Http;

namespace VKProxy.Features;

public interface IForwarderErrorFeature
{/// <summary>
 /// The specified ProxyError.
 /// </summary>
    ForwarderError Error { get; }

    /// <summary>
    /// An Exception that occurred when forwarding the request to the destination, if any.
    /// </summary>
    Exception? Exception { get; }
}

public sealed class ForwarderErrorFeature : IForwarderErrorFeature
{
    public ForwarderErrorFeature(ForwarderError error, Exception? ex)
    {
        Error = error;
        Exception = ex;
    }

    /// <summary>
    /// The specified ForwarderError.
    /// </summary>
    public ForwarderError Error { get; }

    /// <summary>
    /// The error, if any.
    /// </summary>
    public Exception? Exception { get; }
}