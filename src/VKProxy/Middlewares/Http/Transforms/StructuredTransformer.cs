using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using VKProxy.Core.Http;

namespace VKProxy.Middlewares.Http.Transforms;

public class StructuredTransformer : IHttpTransformer
{
    public StructuredTransformer(bool? copyRequestHeaders, bool? copyResponseHeaders, bool? copyResponseTrailers, IList<RequestTransform> requestTransforms, IList<ResponseTransform> responseTransforms, IList<ResponseTrailersTransform> responseTrailerTransforms)
    {
        ShouldCopyRequestHeaders = copyRequestHeaders;
        ShouldCopyResponseHeaders = copyResponseHeaders;
        ShouldCopyResponseTrailers = copyResponseTrailers;
        RequestTransforms = requestTransforms?.ToArray() ?? throw new ArgumentNullException(nameof(requestTransforms));
        ResponseTransforms = responseTransforms?.ToArray() ?? throw new ArgumentNullException(nameof(responseTransforms));
        ResponseTrailerTransforms = responseTrailerTransforms?.ToArray() ?? throw new ArgumentNullException(nameof(responseTrailerTransforms));
    }

    /// <summary>
    /// Indicates if all request headers should be copied to the proxy request before applying transforms.
    /// </summary>
    internal bool? ShouldCopyRequestHeaders { get; }

    /// <summary>
    /// Indicates if all response headers should be copied to the client response before applying transforms.
    /// </summary>
    internal bool? ShouldCopyResponseHeaders { get; }

    /// <summary>
    /// Indicates if all response trailers should be copied to the client response before applying transforms.
    /// </summary>
    internal bool? ShouldCopyResponseTrailers { get; }

    /// <summary>
    /// Request transforms.
    /// </summary>
    internal RequestTransform[] RequestTransforms { get; }

    /// <summary>
    /// Response header transforms.
    /// </summary>
    internal ResponseTransform[] ResponseTransforms { get; }

    /// <summary>
    /// Response trailer transforms.
    /// </summary>
    internal ResponseTrailersTransform[] ResponseTrailerTransforms { get; }

    public async ValueTask TransformRequestAsync(HttpContext context, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
    {
        if (ShouldCopyRequestHeaders.GetValueOrDefault(true))
        {
            foreach (var header in context.Request.Headers)
            {
                var headerName = header.Key;
                var headerValue = header.Value;
                if (RequestUtilities.ShouldSkipRequestHeader(headerName))
                {
                    continue;
                }

                RequestUtilities.AddHeader(proxyRequest, headerName, headerValue);
            }

            // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.3
            // If a message is received with both a Transfer-Encoding and a
            // Content-Length header field, the Transfer-Encoding overrides the
            // Content-Length.  Such a message might indicate an attempt to
            // perform request smuggling (Section 9.5) or response splitting
            // (Section 9.4) and ought to be handled as an error.  A sender MUST
            // remove the received Content-Length field prior to forwarding such
            // a message downstream.
            if (context.Request.Headers.ContainsKey(HeaderNames.TransferEncoding)
                && context.Request.Headers.ContainsKey(HeaderNames.ContentLength))
            {
                proxyRequest.Content?.Headers.Remove(HeaderNames.ContentLength);
            }

            // https://datatracker.ietf.org/doc/html/rfc7540#section-8.1.2.2
            // The only exception to this is the TE header field, which MAY be
            // present in an HTTP/2 request; when it is, it MUST NOT contain any
            // value other than "trailers".
            if (ProtocolHelper.IsHttp2OrGreater(context.Request.Protocol))
            {
                var te = context.Request.Headers.GetCommaSeparatedValues(HeaderNames.TE);
                if (te is not null)
                {
                    for (var i = 0; i < te.Length; i++)
                    {
                        if (string.Equals(te[i], "trailers", StringComparison.OrdinalIgnoreCase))
                        {
                            var added = proxyRequest.Headers.TryAddWithoutValidation(HeaderNames.TE, te[i]);
                            Debug.Assert(added);
                            break;
                        }
                    }
                }
            }
        }

        if (RequestTransforms.Length == 0)
        {
            return;
        }

        var transformContext = new RequestTransformContext()
        {
            DestinationPrefix = destinationPrefix,
            HttpContext = context,
            ProxyRequest = proxyRequest,
            Path = context.Request.Path,
            HeadersCopied = ShouldCopyRequestHeaders.GetValueOrDefault(true),
            CancellationToken = cancellationToken,
        };

        foreach (var requestTransform in RequestTransforms)
        {
            await requestTransform.ApplyAsync(transformContext);

            // The transform generated a response, do not apply further transforms and do not forward.
            if (RequestUtilities.IsResponseSet(context.Response))
            {
                return;
            }
        }

        // Allow a transform to directly set a custom RequestUri.
        if (proxyRequest.RequestUri is null)
        {
            var queryString = transformContext.MaybeQuery?.QueryString ?? context.Request.QueryString;

            proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(
                transformContext.DestinationPrefix, transformContext.Path, queryString);
        }
    }

    public async ValueTask<bool> TransformResponseAsync(HttpContext context, HttpResponseMessage proxyResponse, CancellationToken cancellationToken)
    {
        context.SetResponseTransformed();
        if (ShouldCopyResponseHeaders.GetValueOrDefault(true))
        {
            await TransformResponseAsync(context, proxyResponse);
        }

        if (ResponseTransforms.Length == 0)
        {
            return true;
        }

        var transformContext = new ResponseTransformContext()
        {
            HttpContext = context,
            ProxyResponse = proxyResponse,
            HeadersCopied = ShouldCopyResponseHeaders.GetValueOrDefault(true),
            CancellationToken = cancellationToken,
        };

        foreach (var responseTransform in ResponseTransforms)
        {
            await responseTransform.ApplyAsync(transformContext);
        }

        return !transformContext.SuppressResponseBody;
    }

    private ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse)
    {
        if (proxyResponse is null)
        {
            return new ValueTask<bool>(false);
        }

        var responseHeaders = httpContext.Response.Headers;
        CopyResponseHeaders(proxyResponse.Headers, responseHeaders);
        if (proxyResponse.Content is not null)
        {
            CopyResponseHeaders(proxyResponse.Content.Headers, responseHeaders);
        }

        // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.3
        // If a message is received with both a Transfer-Encoding and a
        // Content-Length header field, the Transfer-Encoding overrides the
        // Content-Length.  Such a message might indicate an attempt to
        // perform request smuggling (Section 9.5) or response splitting
        // (Section 9.4) and ought to be handled as an error.  A sender MUST
        // remove the received Content-Length field prior to forwarding such
        // a message downstream.
        if (proxyResponse.Content is not null
            && proxyResponse.Headers.NonValidated.Contains(HeaderNames.TransferEncoding)
            && proxyResponse.Content.Headers.NonValidated.Contains(HeaderNames.ContentLength))
        {
            httpContext.Response.Headers.Remove(HeaderNames.ContentLength);
        }

        // For responses with status codes that shouldn't include a body,
        // we remove the 'Content-Length: 0' header if one is present.
        if (proxyResponse.Content is not null
            && IsBodylessStatusCode(proxyResponse.StatusCode)
            && proxyResponse.Content.Headers.NonValidated.TryGetValues(HeaderNames.ContentLength, out var contentLengthValue)
            && contentLengthValue.ToString() == "0")
        {
            httpContext.Response.Headers.Remove(HeaderNames.ContentLength);
        }

        return new ValueTask<bool>(true);
    }

    public async ValueTask TransformResponseTrailersAsync(HttpContext context, HttpResponseMessage proxyResponse, CancellationToken cancellationToken)
    {
        if (ShouldCopyResponseTrailers.GetValueOrDefault(true))
        {
            // NOTE: Deliberately not using `context.Response.SupportsTrailers()`, `context.Response.AppendTrailer(...)`
            // because they lookup `IHttpResponseTrailersFeature` for every call. Here we do it just once instead.
            var rresponseTrailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();
            var routgoingTrailers = rresponseTrailersFeature?.Trailers;
            if (routgoingTrailers is not null && !routgoingTrailers.IsReadOnly)
            {
                // Note that trailers, if any, should already have been declared in Proxy's response
                // by virtue of us having proxied all response headers in step 6.
                CopyResponseHeaders(proxyResponse.TrailingHeaders, routgoingTrailers);
            }
        }

        if (ResponseTrailerTransforms.Length == 0)
        {
            return;
        }

        // Only run the transforms if trailers are actually supported by the client response.
        var responseTrailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();
        var outgoingTrailers = responseTrailersFeature?.Trailers;
        if (outgoingTrailers is not null && !outgoingTrailers.IsReadOnly)
        {
            var transformContext = new ResponseTrailersTransformContext()
            {
                HttpContext = context,
                ProxyResponse = proxyResponse,
                HeadersCopied = ShouldCopyResponseTrailers.GetValueOrDefault(true),
                CancellationToken = cancellationToken,
            };

            foreach (var responseTrailerTransform in ResponseTrailerTransforms)
            {
                await responseTrailerTransform.ApplyAsync(transformContext);
            }
        }
    }

    public static void CopyResponseHeaders(HttpHeaders source, IHeaderDictionary destination)
    {
        // We want to append to any prior values, if any.
        // Not using Append here because it skips empty headers.
        foreach (var header in source.NonValidated)
        {
            var headerName = header.Key;
            if (RequestUtilities.ShouldSkipResponseHeader(headerName))
            {
                continue;
            }

            var currentValue = destination[headerName];

            // https://github.com/dotnet/yarp/issues/2269
            // The Strict-Transport-Security may be added by the proxy before forwarding. Only copy the header
            // if it's not already present.
            if (!StringValues.IsNullOrEmpty(currentValue)
                && string.Equals(headerName, HeaderNames.StrictTransportSecurity, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            destination[headerName] = RequestUtilities.Concat(currentValue, header.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBodylessStatusCode(HttpStatusCode statusCode) =>
        statusCode switch
        {
            // A 1xx response is terminated by the end of the header section; it cannot contain content
            // or trailers.
            // See https://www.rfc-editor.org/rfc/rfc9110.html#section-15.2-2
            >= HttpStatusCode.Continue and < HttpStatusCode.OK => true,
            // A 204 response is terminated by the end of the header section; it cannot contain content
            // or trailers.
            // See https://www.rfc-editor.org/rfc/rfc9110.html#section-15.3.5-5
            HttpStatusCode.NoContent => true,
            // Since the 205 status code implies that no additional content will be provided, a server
            // MUST NOT generate content in a 205 response.
            // See https://www.rfc-editor.org/rfc/rfc9110.html#section-15.3.6-3
            HttpStatusCode.ResetContent => true,
            _ => false
        };
}