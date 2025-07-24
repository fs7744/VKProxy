using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VKProxy.ACME.AspNetCore;

internal class HttpChallengeResponseMiddleware : IMiddleware
{
    private readonly IHttpChallengeResponseStore responseStore;
    private readonly ILogger<HttpChallengeResponseMiddleware> logger;

    public HttpChallengeResponseMiddleware(
        IHttpChallengeResponseStore responseStore,
        ILogger<HttpChallengeResponseMiddleware> logger)
    {
        this.responseStore = responseStore;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // assumes that this middleware has been mapped
        var token = context.Request.Path.ToString();
        if (token.StartsWith("/"))
        {
            token = token.Substring(1);
        }

        var value = await responseStore.GetChallengeResponse(token, context.RequestAborted);
        if (string.IsNullOrWhiteSpace(value))
        {
            await next(context);
            return;
        }

        logger.LogDebug("Confirmed challenge request for {token}", token);

        context.Response.ContentLength = value?.Length ?? 0;
        context.Response.ContentType = "application/octet-stream";
        await context.Response.WriteAsync(value!, context.RequestAborted);
        await context.Response.CompleteAsync();
    }
}