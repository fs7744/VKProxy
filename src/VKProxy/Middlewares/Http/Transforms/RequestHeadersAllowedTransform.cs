using Microsoft.Extensions.Primitives;
using System.Collections.Frozen;
using System.Diagnostics;

namespace VKProxy.Middlewares.Http.Transforms;

public class RequestHeadersAllowedTransform : RequestTransform
{
    public RequestHeadersAllowedTransform(string[] allowedHeaders)
    {
        if (allowedHeaders is null)
        {
            throw new ArgumentNullException(nameof(allowedHeaders));
        }

        AllowedHeaders = allowedHeaders;
        AllowedHeadersSet = new HashSet<string>(allowedHeaders, StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    internal string[] AllowedHeaders { get; }

    private FrozenSet<string> AllowedHeadersSet { get; }

    /// <inheritdoc/>
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        Debug.Assert(!context.HeadersCopied);

        foreach (var header in context.HttpContext.Request.Headers)
        {
            var headerName = header.Key;
            var headerValue = header.Value;
            if (!StringValues.IsNullOrEmpty(headerValue)
                && AllowedHeadersSet.Contains(headerName))
            {
                AddHeader(context, headerName, headerValue);
            }
        }

        context.HeadersCopied = true;

        return default;
    }
}