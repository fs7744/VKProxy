using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VKProxy.Core.Adapters;
using VKProxy.Core.Hosting;

namespace CoreDemo;

public class HttpListenHandler : ListenHandlerBase
{
    private readonly IServiceProvider serviceProvider;
    private readonly HttpApplication application;

    public HttpListenHandler(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        application = new HttpApplication(Proxy, serviceProvider.GetRequiredService<IHttpContextFactory>());
    }

    private async Task Proxy(HttpContext context)
    {
        throw new NotImplementedException();
    }

    public override async Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        var a = new HttpConnectionBuilder(serviceProvider);
    }
}