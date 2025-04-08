using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace VKProxy.Core.Adapters;

public static class KestrelExtensions
{
    static KestrelExtensions()
    {
    }

    internal static IServiceCollection UseInternalKestrel(this IServiceCollection services, Action<KestrelServerOptions> options = null)
    {
        services.AddTransient<IHttpContextFactory, DefaultHttpContextFactory>();
        services.TryAddSingleton(typeof(IConnectionFactory), typeof(SocketTransportFactory).Assembly.DefinedTypes.First(i => i.Name == "SocketConnectionFactory"));
        services.AddSingleton<IConnectionListenerFactory, SocketTransportFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>());
        services.Configure<KestrelServerOptions>(o =>
        {
            options?.Invoke(o);
            o.AddServerHeader = false;
        });
        services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
        services.AddTransient<KestrelServer>();
        services.AddSingleton<TransportManagerAdapter>();
        services.AddSingleton<ITransportManager>(i => i.GetRequiredService<TransportManagerAdapter>());
        services.AddSingleton<IHeartbeat>(i => i.GetRequiredService<TransportManagerAdapter>());
        return services;
    }
}