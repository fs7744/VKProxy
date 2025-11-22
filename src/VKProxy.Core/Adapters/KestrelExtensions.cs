using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Quic;
using System.Reflection;
using VKProxy.Core.Config;

namespace VKProxy.Core.Adapters;

public static class KestrelExtensions
{
    private static readonly MethodInfo? TlsHandshakeCallbackOptionsSetHttpProtocolsMethod;
    internal static readonly ConstructorInfo ListenOptionsInitMethod;
    internal static readonly ConstructorInfo EndpointConfigInitMethod;
    internal static readonly ConstructorInfo EndpointConfigInitListMethod;
    internal static readonly MethodInfo ListenOptionsSetEndpointConfig;
    internal static readonly MethodInfo? ListenOptionsSetHttpsCallbackOptions;
    internal static readonly MethodInfo? ListenOptionsSetHttpsOptions;
    internal static readonly ConstructorInfo HttpsConnectionMiddlewareInitMethod;
    internal static readonly MethodInfo HttpsConnectionMiddlewareOnConnectionAsyncMethod;
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
    internal static Type IHeartbeatHandlerType;
    internal static Type IEnumerableIHeartbeatHandlerType;
    internal static readonly object sniConfigDict;

    static KestrelExtensions()
    {
        var types = typeof(KestrelServer).Assembly.GetTypes();
        var sniConfigType = types.First(i => i.Name == "SniConfig");
        sniConfigDict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), sniConfigType));
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
        IHeartbeatHandlerType = types.First(i => i.Name == "IHeartbeatHandler");
        IEnumerableIHeartbeatHandlerType = typeof(IEnumerable<>).MakeGenericType(IHeartbeatHandlerType);

        var httpConnectionBuilderExtensionsType = types.First(i => i.Name == "HttpConnectionBuilderExtensions").GetTypeInfo();
        UseHttpServerMethod = httpConnectionBuilderExtensionsType.DeclaredMethods.First(i => i.Name == "UseHttpServer").MakeGenericMethod(typeof(HttpApplication.Context));
        UseHttp3ServerMethod = httpConnectionBuilderExtensionsType.DeclaredMethods.First(i => i.Name == "UseHttp3Server").MakeGenericMethod(typeof(HttpApplication.Context));
        var typeListenOptions = typeof(ListenOptions).GetTypeInfo();
        ListenOptionsInitMethod = typeListenOptions.DeclaredConstructors.First(i => i.GetParameters().Any(i => i.Name == "endPoint"));
        ListenOptionsSetEndpointConfig = typeListenOptions.DeclaredProperties.First(i => i.Name == "EndpointConfig").SetMethod;
        ListenOptionsSetHttpsCallbackOptions = typeListenOptions.DeclaredProperties.First(i => i.Name == "HttpsCallbackOptions").SetMethod;
        ListenOptionsSetHttpsOptions = typeListenOptions.DeclaredProperties.First(i => i.Name == "HttpsOptions").SetMethod;
        TlsHandshakeCallbackOptionsSetHttpProtocolsMethod = typeof(TlsHandshakeCallbackOptions).GetTypeInfo().DeclaredProperties.First(i => i.Name == "HttpProtocols").SetMethod;
        var typeHttpsConnectionMiddleware = types.First(i => i.Name == "HttpsConnectionMiddleware").GetTypeInfo();
        HttpsConnectionMiddlewareInitMethod = typeHttpsConnectionMiddleware.DeclaredConstructors.First(i =>
        {
            var p = i.GetParameters();
            return p.Length == 5 && p.Any(i => i.ParameterType == typeof(HttpsConnectionAdapterOptions));
        });
        HttpsConnectionMiddlewareOnConnectionAsyncMethod = typeHttpsConnectionMiddleware.DeclaredMethods.First(i => i.Name == "OnConnectionAsync");
    }

    public static void SetHttpProtocols(this TlsHandshakeCallbackOptions options, HttpProtocols protocols)
    {
        TlsHandshakeCallbackOptionsSetHttpProtocolsMethod.Invoke(options, new object[] { protocols });
    }

    internal static object InitEndpointConfig(string key, string url, IConfigurationSection section)
    {
        return EndpointConfigInitMethod.Invoke(new object[] { key, url, sniConfigDict, section });
    }

    internal static ListenOptions InitListenOptions(EndPoint endPoint, object endpointConfig)
    {
        var r = ListenOptionsInitMethod.Invoke(new object[] { endPoint }) as ListenOptions;
        if (endpointConfig != null)
        {
            ListenOptionsSetEndpointConfig.Invoke(r, new object[] { endpointConfig });
        }
        return r;
    }

    internal static IServiceCollection UseInternalKestrel(this IServiceCollection services, Action<KestrelServerOptions> options = null)
    {
        services.UseInternalKestrelCore();
        services.AddTransient<IHttpContextFactory, DefaultHttpContextFactory>();
        if (QuicListener.IsSupported)
            services.TryAddSingleton(typeof(IMultiplexedConnectionListenerFactory), typeof(QuicTransportOptions).Assembly.DefinedTypes.First(i => i.Name == "QuicTransportFactory"));
        if (OperatingSystem.IsWindows())
        {
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton(typeof(IConnectionListenerFactory), typeof(NamedPipeTransportOptions).Assembly.DefinedTypes.First(i => i.Name == "NamedPipeTransportFactory"));
        }
        services.AddSingleton<IConnectionListenerFactory, SocketTransportFactory>();
        services.Configure<KestrelServerOptions>(o =>
        {
            options?.Invoke(o);
            o.AddServerHeader = false;
        });
        return services;
    }

    public static IServiceCollection UseInternalKestrelCore(this IServiceCollection services)
    {
        services.TryAddSingleton(typeof(IConnectionFactory), typeof(SocketTransportFactory).Assembly.DefinedTypes.First(i => i.Name == "SocketConnectionFactory"));
        services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
        services.AddTransient<KestrelServer>();
        services.AddSingleton<TransportManagerAdapter>();
        services.AddSingleton<ITransportManager>(i => i.GetRequiredService<TransportManagerAdapter>());
        services.AddSingleton<IHeartbeat>(i => i.GetRequiredService<TransportManagerAdapter>());
        return services;
    }

    public static Task BindHttpAsync(this ITransportManager transportManager, EndPointOptions options, RequestDelegate requestDelegate, CancellationToken cancellationToken, HttpProtocols protocols = HttpProtocols.Http1AndHttp2AndHttp3, bool addAltSvcHeader = true, Action<IConnectionBuilder> config = null
    , Action<IMultiplexedConnectionBuilder> configMultiplexed = null, HttpsConnectionAdapterOptions callbackOptions = null)
    {
        return transportManager.BindHttpApplicationAsync(options, new HttpApplication(requestDelegate, transportManager.ServiceProvider.GetRequiredService<IHttpContextFactory>()),
            cancellationToken, protocols, addAltSvcHeader, config, configMultiplexed, callbackOptions);
    }
}