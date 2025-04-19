using Microsoft.AspNetCore.Http;

namespace VKProxy.Middlewares.Http.Transforms;

public interface IHttpTransformer
{
    ValueTask TransformRequestAsync(HttpContext context, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken);

    ValueTask<bool> TransformResponseAsync(HttpContext context, HttpResponseMessage proxyResponse, CancellationToken cancellationToken);

    ValueTask TransformResponseTrailersAsync(HttpContext context, HttpResponseMessage proxyResponse, CancellationToken cancellationToken);
}