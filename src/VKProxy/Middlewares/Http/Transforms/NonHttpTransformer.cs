using Microsoft.AspNetCore.Http;

namespace VKProxy.Middlewares.Http.Transforms;

public class NonHttpTransformer : IHttpTransformer
{
    private readonly IHttpTransformer transformer;

    public NonHttpTransformer(IHttpTransformer transformer)
    {
        this.transformer = transformer;
    }

    public ValueTask TransformRequestAsync(HttpContext context, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
    {
        return transformer.TransformRequestAsync(context, proxyRequest, destinationPrefix, cancellationToken);
    }

    public ValueTask<bool> TransformResponseAsync(HttpContext context, HttpResponseMessage proxyResponse, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(false);
    }

    public ValueTask TransformResponseTrailersAsync(HttpContext context, HttpResponseMessage proxyResponse, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}