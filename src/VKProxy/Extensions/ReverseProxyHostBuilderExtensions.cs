using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Quic;
using VKProxy;
using VKProxy.Config;
using VKProxy.Config.Validators;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;

namespace Microsoft.Extensions.Hosting;

public static class ReverseProxyHostBuilderExtensions
{
    public static IHostBuilder UseReverseProxy(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseVKProxyCore();
        hostBuilder.ConfigureServices(services =>
        {
            if (QuicListener.IsSupported)
            {
                services.AddTransient<IConfigureOptions<QuicTransportOptions>, QuicTransportOptionsSetup>();
            }
            if (OperatingSystem.IsWindows())
            {
                services.AddTransient<IConfigureOptions<NamedPipeTransportOptions>, NamedPipeTransportOptionsSetup>();
            }
            services.AddTransient<IConfigureOptions<UdpSocketTransportOptions>, UdpSocketTransportOptionsSetup>();
            services.AddTransient<IConfigureOptions<ReverseProxyOptions>, ReverseProxyOptionsSetup>();
            services.AddTransient<IConfigureOptions<SocketTransportOptions>, SocketTransportOptionsSetup>();
            services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
            services.AddSingleton<IListenHandler, ListenHandler>();
            services.AddSingleton<IConfigSource<IProxyConfig>, ProxyConfigSource>();
            services.AddSingleton<ProxyLogger>();
            services.AddSingleton<IValidator<IProxyConfig>, ProxyConfigValidator>();
            services.AddSingleton<IValidator<ListenConfig>, ListenConfigValidator>();
            services.AddSingleton<IValidator<SniConfig>, SniConfigValidator>();
            services.AddSingleton<IEndPointConvertor, CommonEndPointConvertor>();
            services.AddSingleton<ISniSelector, SniSelector>();
        });

        return hostBuilder;
    }
}