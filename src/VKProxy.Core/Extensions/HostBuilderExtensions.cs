using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Reflection;
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
        services.AddSingleton<IMemoryPoolFactory<byte>>(i => i.GetRequiredService<IMemoryPoolSizeFactory<byte>>());
        services.AddSingleton<IUdpConnectionFactory, UdpConnectionFactory>();
        services.AddSingleton<IConnectionListenerFactory, UdpTransportFactory>();
        services.AddSingleton<UdpMetrics>();
        services.AddSingleton<GeneralLogger>();
        services.AddSingleton<IHostedService, VKHostedService>();
        services.TryAddSingleton<IServer, VKServer>();
        services.AddSingleton<ICertificateLoader, CertificateLoader>();
        services.AddSingleton<IRandomFactory, RandomFactory>();

#if NET9_0
        services.AddTransient<IConfigureOptions<SocketTransportOptions>, SocketTransportOptionsSetup>();
        return services;
    }

    internal sealed class SocketTransportOptionsSetup : IConfigureOptions<SocketTransportOptions>
    {
        private readonly IMemoryPoolFactory<byte> factory;

        public SocketTransportOptionsSetup(IMemoryPoolFactory<byte> factory)
        {
            this.factory = factory;
        }

        public void Configure(SocketTransportOptions options)
        {
            SetSocketTransportOptionsMemoryPool(options, () => factory.Create());
        }
    }

    internal static readonly Action<SocketTransportOptions, Func<MemoryPool<byte>>> SetSocketTransportOptionsMemoryPool = typeof(SocketTransportOptions).GetTypeInfo().DeclaredProperties.First(i => i.Name == "MemoryPoolFactory").SetMethod.CreateDelegate<Action<SocketTransportOptions, Func<MemoryPool<byte>>>>();

# else
        return services;
    }
#endif
}