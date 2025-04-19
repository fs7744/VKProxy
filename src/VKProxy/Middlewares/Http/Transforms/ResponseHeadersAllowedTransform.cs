﻿using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Net.Http.Headers;
using VKProxy.Core.Http;

namespace VKProxy.Middlewares.Http.Transforms;

public class ResponseHeadersAllowedTransform : ResponseTransform
{
    public ResponseHeadersAllowedTransform(string[] allowedHeaders)
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
    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.ProxyResponse is null)
        {
            return default;
        }

        Debug.Assert(!context.HeadersCopied);

        // See https://github.com/dotnet/yarp/blob/51d797986b1fea03500a1ad173d13a1176fb5552/src/ReverseProxy/Forwarder/HttpTransformer.cs#L67-L77
        var responseHeaders = context.HttpContext.Response.Headers;
        CopyResponseHeaders(context.ProxyResponse.Headers, responseHeaders);
        if (context.ProxyResponse.Content is not null)
        {
            CopyResponseHeaders(context.ProxyResponse.Content.Headers, responseHeaders);
        }

        context.HeadersCopied = true;

        return default;
    }

    // See https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Forwarder/HttpTransformer.cs#:~:text=void-,CopyResponseHeaders
    private void CopyResponseHeaders(HttpHeaders source, IHeaderDictionary destination)
    {
        foreach (var header in source.NonValidated)
        {
            var headerName = header.Key;
            if (!AllowedHeadersSet.Contains(headerName))
            {
                continue;
            }

            destination[headerName] = RequestUtilities.Concat(destination[headerName], header.Value);
        }
    }
}