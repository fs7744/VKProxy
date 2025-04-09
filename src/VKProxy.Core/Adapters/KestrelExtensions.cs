using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Net.Quic;
using System.Reflection;

namespace VKProxy.Core.Adapters;

internal static class KestrelExtensions
{
    internal static readonly ConstructorInfo EndpointConfigInitMethod;
    internal static readonly ConstructorInfo EndpointConfigInitListMethod;
    internal static readonly MethodInfo UseHttpServerMethod;
    internal static readonly MethodInfo UseHttp3ServerMethod;
    internal static readonly Type TransportManagerType;
    internal static readonly Type HttpsConfigurationServiceType;
    internal static readonly Type HttpsConnectionMiddlewareType;
    internal static readonly Type KestrelServerImplType;
    internal static readonly Type ServiceContextType;
    internal static readonly Type HeartbeatType;
    internal static readonly Type KestrelMetricsType;
    internal static readonly Type DummyMeterFactoryType;

    static KestrelExtensions()
    {
        var types = typeof(KestrelServer).Assembly.GetTypes();
        var typeEndpointConfig = types.First(i => i.Name == "EndpointConfig");
        EndpointConfigInitMethod = typeEndpointConfig.GetTypeInfo().DeclaredConstructors.First();
        var list = typeof(List<>).MakeGenericType(typeEndpointConfig).GetTypeInfo();
        EndpointConfigInitListMethod = list.DeclaredConstructors.First(i => i.GetParameters().Length == 0);

        TransportManagerType = types.First(i => i.Name == "TransportManager");
        HttpsConfigurationServiceType = types.First(i => i.Name == "HttpsConfigurationService");
        HttpsConnectionMiddlewareType = types.First(i => i.Name == "HttpsConnectionMiddleware");
        KestrelServerImplType = types.First(i => i.Name == "KestrelServerImpl");
        ServiceContextType = types.First(i => i.Name == "ServiceContext");
        HeartbeatType = types.First(i => i.Name == "Heartbeat");
        KestrelMetricsType = types.First(i => i.Name == "KestrelMetrics");
        DummyMeterFactoryType = types.First(i => i.Name == "DummyMeterFactory");

        var httpConnectionBuilderExtensionsType = types.First(i => i.Name == "HttpConnectionBuilderExtensions").GetTypeInfo();
        UseHttpServerMethod = httpConnectionBuilderExtensionsType.DeclaredMethods.First(i => i.Name == "UseHttpServer").MakeGenericMethod(typeof(HttpApplication.Context));
        UseHttp3ServerMethod = httpConnectionBuilderExtensionsType.DeclaredMethods.First(i => i.Name == "UseHttp3Server").MakeGenericMethod(typeof(HttpApplication.Context));
    }

    internal static object InitEndpointConfig(string key, string url, IConfigurationSection section)
    {
        return EndpointConfigInitMethod.Invoke(new object[] { key, url, null, section });
    }

    internal static IServiceCollection UseInternalKestrel(this IServiceCollection services, Action<KestrelServerOptions> options = null)
    {
        services.AddTransient<IHttpContextFactory, DefaultHttpContextFactory>();
        services.TryAddSingleton(typeof(IConnectionFactory), typeof(SocketTransportFactory).Assembly.DefinedTypes.First(i => i.Name == "SocketConnectionFactory"));
        if (QuicListener.IsSupported)
            services.TryAddSingleton(typeof(IMultiplexedConnectionListenerFactory), typeof(QuicTransportOptions).Assembly.DefinedTypes.First(i => i.Name == "QuicTransportFactory"));
        if (OperatingSystem.IsWindows())
        {
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton(typeof(IConnectionListenerFactory), typeof(NamedPipeTransportOptions).Assembly.DefinedTypes.First(i => i.Name == "NamedPipeTransportFactory"));
        }
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
        services.AddSingleton<IHttpServerBuilder>(i => i.GetRequiredService<TransportManagerAdapter>());
        return services;
    }
}