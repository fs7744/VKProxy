using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VKProxy.Features;

namespace ProxyDemo;

internal class EchoHttpMiddleware : IMiddleware
{
    private readonly ILogger<EchoHttpMiddleware> logger;

    public EchoHttpMiddleware(ILogger<EchoHttpMiddleware> logger)
    {
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var req = context.Request;
        logger.LogInformation($"begin {req.Protocol} {req.Host} {req.Path} {DateTime.Now}");
        await next(context);
        logger.LogInformation($"end {req.Protocol} {req.Host} {req.Path} {DateTime.Now}");
    }
}