﻿using Microsoft.Extensions.Primitives;

namespace VKProxy.Middlewares.Http.Transforms;

public class ResponseHeaderValueTransform : ResponseTransform
{
    public ResponseHeaderValueTransform(string headerName, string value, bool append, ResponseCondition condition)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        HeaderName = headerName;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Append = append;
        Condition = condition;
    }

    internal ResponseCondition Condition { get; }

    internal bool Append { get; }

    internal string HeaderName { get; }

    internal string Value { get; }

    // Assumes the response status code has been set on the HttpContext already.
    /// <inheritdoc/>
    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (Condition == ResponseCondition.Always
            || Success(context) == (Condition == ResponseCondition.Success))
        {
            if (Append)
            {
                var existingHeader = TakeHeader(context, HeaderName);
                var value = StringValues.Concat(existingHeader, Value);
                SetHeader(context, HeaderName, value);
            }
            else
            {
                SetHeader(context, HeaderName, Value);
            }
        }

        return default;
    }
}