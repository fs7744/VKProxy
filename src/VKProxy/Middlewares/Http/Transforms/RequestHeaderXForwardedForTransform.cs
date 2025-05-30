﻿using Microsoft.Extensions.Primitives;
using System.Diagnostics;

namespace VKProxy.Middlewares.Http.Transforms;

/// <summary>
/// Sets or appends the X-Forwarded-For header with the previous client's IP address.
/// </summary>
public class RequestHeaderXForwardedForTransform : RequestTransform
{
    /// <summary>
    /// Creates a new transform.
    /// </summary>
    /// <param name="headerName">The header name.</param>
    /// <param name="action">Action to applied to the header.</param>
    public RequestHeaderXForwardedForTransform(string headerName, ForwardedTransformActions action)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        HeaderName = headerName;
        Debug.Assert(action != ForwardedTransformActions.Off);
        TransformAction = action;
    }

    internal string HeaderName { get; }

    internal ForwardedTransformActions TransformAction { get; }

    /// <inheritdoc/>
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        string? remoteIp = null;
        var remoteIpAddress = context.HttpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress is not null)
        {
            remoteIp = remoteIpAddress.IsIPv4MappedToIPv6 ?
                remoteIpAddress.MapToIPv4().ToString() :
                remoteIpAddress.ToString();
        }

        switch (TransformAction)
        {
            case ForwardedTransformActions.Set:
                RemoveHeader(context, HeaderName);
                if (remoteIp is not null)
                {
                    AddHeader(context, HeaderName, remoteIp);
                }
                break;

            case ForwardedTransformActions.Append:
                Append(context, remoteIp);
                break;

            case ForwardedTransformActions.Remove:
                RemoveHeader(context, HeaderName);
                break;

            default:
                throw new NotImplementedException(TransformAction.ToString());
        }

        return default;
    }

    private void Append(RequestTransformContext context, string? remoteIp)
    {
        var existingValues = TakeHeader(context, HeaderName);
        if (remoteIp is null)
        {
            if (!string.IsNullOrEmpty(existingValues))
            {
                AddHeader(context, HeaderName, existingValues);
            }
        }
        else
        {
            var values = StringValues.Concat(existingValues, remoteIp);
            AddHeader(context, HeaderName, values);
        }
    }
}