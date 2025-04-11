using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VKProxy;
using VKProxy.Config;
using VKProxy.Core.Hosting;

namespace Microsoft.Extensions.Hosting;

public static class ReverseProxyHostBuilderExtensions
{
    public static IHostBuilder UseReverseProxy(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseVKProxyCore();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
            services.AddSingleton<IListenHandler, ListenHandler>();
        });

        return hostBuilder;
    }
}