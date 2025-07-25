using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VKProxy.Core.Adapters;
using VKProxy.Core.Buffers;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Infrastructure;
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
            services.UseVKProxyCore();
        });

        return hostBuilder;
    }

    public static IHostApplicationBuilder UseVKProxyCore(this IHostApplicationBuilder hostBuilder)
    {
        var services = hostBuilder.Services;

        services.UseInternalKestrelCore();
        services.UseVKProxyCore();

        return hostBuilder;
    }

    public static IServiceCollection UseVKProxyCore(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryPoolSizeFactory<byte>, PinnedBlockMemoryPoolFactory>();
        services.AddSingleton<IUdpConnectionFactory, UdpConnectionFactory>();
        services.AddSingleton<IConnectionListenerFactory, UdpTransportFactory>();
        services.AddSingleton<GeneralLogger>();
        services.AddSingleton<IHostedService, VKHostedService>();
        services.TryAddSingleton<IServer, VKServer>();
        services.AddSingleton<ICertificateLoader, CertificateLoader>();
        services.AddSingleton<IRandomFactory, RandomFactory>();

        return services;
    }
}