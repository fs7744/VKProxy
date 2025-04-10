using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Core.Sockets.Udp.Client;

namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseVKProxyCore(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.UseInternalKestrel();
            services.AddSingleton<IUdpConnectionFactory, UdpConnectionFactory>();
            services.AddSingleton<IConnectionListenerFactory, UdpTransportFactory>();
            services.AddSingleton<GeneralLogger>();
            services.AddSingleton<IHostedService, VKHostedService>();
            services.TryAddSingleton<IServer, VKServer>();
            services.AddSingleton<ICertificateLoader, CertificateLoader>();
        });

        return hostBuilder;
    }
}