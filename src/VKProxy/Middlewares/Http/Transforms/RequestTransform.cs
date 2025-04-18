using Microsoft.Extensions.Primitives;
using VKProxy.Core.Http;

namespace VKProxy.Middlewares.Http.Transforms;

public abstract class RequestTransform
{
    public abstract ValueTask ApplyAsync(RequestTransformContext context);

    public static StringValues TakeHeader(RequestTransformContext context, string headerName)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        var proxyRequest = context.ProxyRequest;

        if (RequestUtilities.TryGetValues(proxyRequest.Headers, headerName, out var existingValues))
        {
            proxyRequest.Headers.Remove(headerName);
        }
        else if (proxyRequest.Content is { } content && RequestUtilities.TryGetValues(content.Headers, headerName, out existingValues))
        {
            content.Headers.Remove(headerName);
        }
        else if (!context.HeadersCopied)
        {
            existingValues = context.HttpContext.Request.Headers[headerName];
        }

        return existingValues;
    }

    /// <summary>
    /// Adds the given header to the HttpRequestMessage or HttpContent where applicable.
    /// </summary>
    public static void AddHeader(RequestTransformContext context, string headerName, StringValues values)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        RequestUtilities.AddHeader(context.ProxyRequest, headerName, values);
    }

    /// <summary>
    /// Removes the given header from the HttpRequestMessage or HttpContent where applicable.
    /// </summary>
    public static void RemoveHeader(RequestTransformContext context, string headerName)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        RequestUtilities.RemoveHeader(context.ProxyRequest, headerName);
    }
}