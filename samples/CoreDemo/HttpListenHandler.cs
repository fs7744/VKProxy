using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;

namespace CoreDemo;

public class HttpListenHandler : ListenHandlerBase
{
    private readonly IServiceProvider serviceProvider;
    private readonly IHttpServerBuilder httpServerBuilder;
    private readonly ILogger<HttpListenHandler> logger;
    private readonly HttpApplication application;

    public HttpListenHandler(IServiceProvider serviceProvider, IHttpServerBuilder httpServerBuilder, ILogger<HttpListenHandler> logger)
    {
        this.serviceProvider = serviceProvider;
        this.httpServerBuilder = httpServerBuilder;
        this.logger = logger;
        application = new HttpApplication(Proxy, serviceProvider.GetRequiredService<IHttpContextFactory>());
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
        var a = new HttpConnectionBuilder(serviceProvider);
        httpServerBuilder.UseHttpServer(a, application, HttpProtocols.Http1, true);
        try
        {
            var ip = new EndPointOptions()
            {
                EndPoint = IPEndPoint.Parse("127.0.0.1:5000"),
                Key = "http"
            };
            await transportManager.BindAsync(ip, a.Build(), cancellationToken);
            logger.LogInformation($"listen {ip.EndPoint}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
}