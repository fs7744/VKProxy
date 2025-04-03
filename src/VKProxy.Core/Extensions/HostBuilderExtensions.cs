using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VKProxy.Core.Extensions;
using VKProxy.Core.Hosting;

namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseVKProxyCore(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.UseInternalKestrel();
            services.TryAddSingleton<IConnectionListenerFactory, SocketTransportFactory>();
            services.AddSingleton<IHostedService, VKHostedService>();
            services.TryAddSingleton<IServer, VKServer>();
        });

        return hostBuilder;
    }
}