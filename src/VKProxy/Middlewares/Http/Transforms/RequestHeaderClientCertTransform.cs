﻿namespace VKProxy.Middlewares.Http.Transforms;

/// <summary>
/// Base64 encodes the client certificate (if any) and sets it as the header value.
/// </summary>
public class RequestHeaderClientCertTransform : RequestTransform
{
    public RequestHeaderClientCertTransform(string headerName)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        HeaderName = headerName;
    }

    internal string HeaderName { get; }

    /// <inheritdoc/>
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        RemoveHeader(context, HeaderName);

        var clientCert = context.HttpContext.Connection.ClientCertificate;
        if (clientCert is not null)
        {
            var encoded = Convert.ToBase64String(clientCert.RawData);
            AddHeader(context, HeaderName, encoded);
        }

        return default;
    }
}