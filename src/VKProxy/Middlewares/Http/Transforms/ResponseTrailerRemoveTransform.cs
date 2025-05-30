﻿using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

namespace VKProxy.Middlewares.Http.Transforms;

public class ResponseTrailerRemoveTransform : ResponseTrailersTransform
{
    public ResponseTrailerRemoveTransform(string headerName, ResponseCondition condition)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        HeaderName = headerName;
        Condition = condition;
    }

    internal string HeaderName { get; }

    internal ResponseCondition Condition { get; }

    // Assumes the response status code has been set on the HttpContext already.
    /// <inheritdoc/>
    public override ValueTask ApplyAsync(ResponseTrailersTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        Debug.Assert(context.ProxyResponse is not null);

        if (Condition == ResponseCondition.Always
            || Success(context) == (Condition == ResponseCondition.Success))
        {
            var responseTrailersFeature = context.HttpContext.Features.Get<IHttpResponseTrailersFeature>();
            var responseTrailers = responseTrailersFeature?.Trailers;
            // Support should have already been checked by the caller.
            Debug.Assert(responseTrailers is not null);
            Debug.Assert(!responseTrailers.IsReadOnly);

            responseTrailers.Remove(HeaderName);
        }

        return default;
    }
}