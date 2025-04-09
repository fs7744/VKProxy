using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;

namespace CoreDemo;

public class HttpListenHandler : ListenHandlerBase
{
    private readonly ILogger<HttpListenHandler> logger;

    public HttpListenHandler(ILogger<HttpListenHandler> logger)
    {
        this.logger = logger;
    }

    private async Task Proxy(HttpContext context)
    {
        var resp = context.Response;
        resp.StatusCode = 404;
        await resp.WriteAsJsonAsync(new { context.Request.Protocol });
        await resp.CompleteAsync().ConfigureAwait(false);
    }

    public override async Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        try
        {
            var ip = new EndPointOptions()
            {
                EndPoint = IPEndPoint.Parse("127.0.0.1:5000"),
                Key = "http"
            };
            await transportManager.BindHttpAsync(ip, Proxy, false, cancellationToken);
            logger.LogInformation($"listen {ip.EndPoint}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
}