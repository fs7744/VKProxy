using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VKProxy.Core.Adapters;
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
            services.AddSingleton<TransportManagerAdapter>();
        });

        return hostBuilder;
    }
}