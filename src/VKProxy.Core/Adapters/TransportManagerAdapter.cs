using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using VKProxy.Core.Config;

namespace VKProxy.Core.Adapters;

public class TransportManagerAdapter : ITransportManager, IHeartbeat
{
    private static MethodInfo StopAsyncMethod;
    private static MethodInfo StopEndpointsAsyncMethod;
    private static MethodInfo MultiplexedBindAsyncMethod;
    private static MethodInfo BindAsyncMethod;
    private static MethodInfo StartHeartbeatMethod;
    private object transportManager;
    private object heartbeat;
    private object serviceContext;
    private object metrics;
    private int multiplexedTransportCount;
    private int transportCount;
    internal readonly IServiceProvider serviceProvider;

    IServiceProvider ITransportManager.ServiceProvider => serviceProvider;

    public TransportManagerAdapter(IServiceProvider serviceProvider, IEnumerable<IConnectionListenerFactory> transportFactories, IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedConnectionListenerFactories)
    {
        (transportManager, heartbeat, serviceContext, metrics) = CreateTransportManager(serviceProvider);
        multiplexedTransportCount = multiplexedConnectionListenerFactories.Count();
        transportCount = transportFactories.Count();
        this.serviceProvider = serviceProvider;
    }

    private static (object, object, object, object) CreateTransportManager(IServiceProvider serviceProvider)
    {
        foreach (var item in KestrelExtensions.TransportManagerType.GetTypeInfo().DeclaredMethods)
        {
            if (item.Name == "StopAsync")
            {
                StopAsyncMethod = item;
            }
            else if (item.Name == "StopEndpointsAsync")
            {
                StopEndpointsAsyncMethod = item;
            }
            else if (item.Name == "BindAsync")
            {
                if (item.GetParameters().Any(i => i.ParameterType == typeof(ConnectionDelegate)))
                {
                    BindAsyncMethod = item;
                }
                else
                {
                    MultiplexedBindAsyncMethod = item;
                }
            }
        }

        var s = CreateServiceContext(serviceProvider);
        var r = Activator.CreateInstance(KestrelExtensions.TransportManagerType,
                    Enumerable.Reverse(serviceProvider.GetServices<IConnectionListenerFactory>()).ToList(),
                    Enumerable.Reverse(serviceProvider.GetServices<IMultiplexedConnectionListenerFactory>()).ToList(),
                    CreateHttpsConfigurationService(serviceProvider),
                    s.context
                    );
        return (r, s.heartbeat, s.context, s.metrics);

        static object CreateHttpsConfigurationService(IServiceProvider serviceProvider)
        {
            var CreateLogger = typeof(LoggerFactoryExtensions).GetTypeInfo().DeclaredMethods.First(i => i.Name == "CreateLogger" && i.ContainsGenericParameters);
            var r = Activator.CreateInstance(KestrelExtensions.HttpsConfigurationServiceType);
            var m = KestrelExtensions.HttpsConfigurationServiceType.GetMethod("Initialize");
            var log = serviceProvider.GetRequiredService<ILoggerFactory>();
            var l = CreateLogger.MakeGenericMethod(KestrelExtensions.HttpsConnectionMiddlewareType).Invoke(null, new object[] { log });
            m.Invoke(r, new object[] { serviceProvider.GetRequiredService<IHostEnvironment>(), log.CreateLogger<KestrelServer>(), l });
            return r;
        }

        static (object context, object heartbeat, object metrics) CreateServiceContext(IServiceProvider serviceProvider)
        {
            var m = CreateKestrelMetrics();
            var KestrelCreateServiceContext = KestrelExtensions.KestrelServerImplType.GetMethod("CreateServiceContext", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var r = KestrelCreateServiceContext.Invoke(null, new object[]
            {
                serviceProvider.GetRequiredService<IOptions<KestrelServerOptions>>(),
                serviceProvider.GetRequiredService<ILoggerFactory>(),
                null,
                m
            });
            var h = KestrelExtensions.ServiceContextType.GetTypeInfo().DeclaredProperties.First(i => i.Name == "Heartbeat");
            StartHeartbeatMethod = KestrelExtensions.HeartbeatType.GetTypeInfo().DeclaredMethods.First(i => i.Name == "Start");
            return (r, h.GetGetMethod().Invoke(r, null), m);
        }

        static object CreateKestrelMetrics()
        {
            return Activator.CreateInstance(KestrelExtensions.KestrelMetricsType, Activator.CreateInstance(KestrelExtensions.DummyMeterFactoryType));
        }
    }

    public Task<EndPoint> BindAsync(EndPointOptions endpointConfig, ConnectionDelegate connectionDelegate, CancellationToken cancellationToken)
    {
        return BindAsyncMethod.Invoke(transportManager, new object[] { endpointConfig.EndPoint, connectionDelegate, endpointConfig.Init(), cancellationToken }) as Task<EndPoint>;
    }

    public Task<EndPoint> BindAsync(EndPointOptions endpointConfig, MultiplexedConnectionDelegate multiplexedConnectionDelegate, CancellationToken cancellationToken)
    {
        return MultiplexedBindAsyncMethod.Invoke(transportManager, new object[] { endpointConfig.EndPoint, multiplexedConnectionDelegate, endpointConfig.GetListenOptions(), cancellationToken }) as Task<EndPoint>;
    }

    public Task StopEndpointsAsync(List<EndPointOptions> endpointsToStop, CancellationToken cancellationToken)
    {
        return StopEndpointsAsyncMethod.Invoke(transportManager, new object[] { EndPointOptions.Init(endpointsToStop), cancellationToken }) as Task;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopAsyncMethod.Invoke(transportManager, new object[] { cancellationToken }) as Task;
    }

    public void StartHeartbeat()
    {
        if (heartbeat != null)
        {
            StartHeartbeatMethod.Invoke(heartbeat, null);
        }
    }

    public void StopHeartbeat()
    {
        if (heartbeat is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public IConnectionBuilder UseHttpServer(IConnectionBuilder builder, IHttpApplication<HttpApplication.Context> application, HttpProtocols protocols, bool addAltSvcHeader)
    {
        KestrelExtensions.UseHttpServerMethod.Invoke(null, new object[] { builder, serviceContext, application, protocols, addAltSvcHeader });
        return builder;
    }

    public IMultiplexedConnectionBuilder UseHttp3Server(IMultiplexedConnectionBuilder builder, IHttpApplication<HttpApplication.Context> application, HttpProtocols protocols, bool addAltSvcHeader)
    {
        KestrelExtensions.UseHttp3ServerMethod.Invoke(null, new object[] { builder, serviceContext, application, protocols, addAltSvcHeader });
        return builder;
    }

    public ConnectionDelegate UseHttps(ConnectionDelegate next, HttpsConnectionAdapterOptions tlsCallbackOptions, HttpProtocols protocols)
    {
        if (tlsCallbackOptions == null)
            return next;
        var o = KestrelExtensions.HttpsConnectionMiddlewareInitMethod.Invoke(new object[] { next, tlsCallbackOptions, protocols, serviceProvider.GetRequiredService<ILoggerFactory>(), metrics });
        return KestrelExtensions.HttpsConnectionMiddlewareOnConnectionAsyncMethod.CreateDelegate<ConnectionDelegate>(o);
    }

    public async Task BindHttpApplicationAsync(EndPointOptions options, IHttpApplication<HttpApplication.Context> application, CancellationToken cancellationToken, HttpProtocols protocols = HttpProtocols.Http1AndHttp2AndHttp3, bool addAltSvcHeader = true, Action<IConnectionBuilder> config = null
        , Action<IMultiplexedConnectionBuilder> configMultiplexed = null, HttpsConnectionAdapterOptions callbackOptions = null)
    {
        var hasHttp1 = protocols.HasFlag(HttpProtocols.Http1);
        var hasHttp2 = protocols.HasFlag(HttpProtocols.Http2);
        var hasHttp3 = protocols.HasFlag(HttpProtocols.Http3);
        var hasTls = callbackOptions is not null;

        if (hasTls)
        {
            if (hasHttp3)
            {
                options.GetListenOptions().Protocols = protocols;
                options.SetHttpsOptions(callbackOptions);
            }
            //callbackOptions.SetHttpProtocols(protocols);
            //if (hasHttp3)
            //{
            //    HttpsConnectionAdapterOptions
            //    options.SetHttpsCallbackOptions(callbackOptions);
            //}
        }
        else
        {
            // Http/1 without TLS, no-op HTTP/2 and 3.
            if (hasHttp1)
            {
                hasHttp2 = false;
                hasHttp3 = false;
            }
            // Http/3 requires TLS. Note we only let it fall back to HTTP/1, not HTTP/2
            else if (hasHttp3)
            {
                throw new InvalidOperationException("HTTP/3 requires HTTPS.");
            }
        }

        // Quic isn't registered if it's not supported, throw if we can't fall back to 1 or 2
        if (hasHttp3 && multiplexedTransportCount == 0 && !(hasHttp1 || hasHttp2))
        {
            throw new InvalidOperationException("Unable to bind an HTTP/3 endpoint. This could be because QUIC has not been configured using UseQuic, or the platform doesn't support QUIC or HTTP/3.");
        }

        addAltSvcHeader = addAltSvcHeader && multiplexedTransportCount > 0;

        // Add the HTTP middleware as the terminal connection middleware
        if (hasHttp1 || hasHttp2
            || protocols == HttpProtocols.None)
        {
            if (transportCount == 0)
            {
                throw new InvalidOperationException($"Cannot start HTTP/1.x or HTTP/2 server if no {nameof(IConnectionListenerFactory)} is registered.");
            }

            var builder = new HttpConnectionBuilder(serviceProvider);
            config?.Invoke(builder);
            UseHttpServer(builder, application, protocols, addAltSvcHeader);
            var connectionDelegate = UseHttps(builder.Build(), callbackOptions, protocols);

            options.EndPoint = await BindAsync(options, connectionDelegate, cancellationToken).ConfigureAwait(false);
        }

        if (hasHttp3 && multiplexedTransportCount > 0)
        {
            var builder = new HttpMultiplexedConnectionBuilder(serviceProvider);
            configMultiplexed?.Invoke(builder);
            UseHttp3Server(builder, application, protocols, addAltSvcHeader);
            var multiplexedConnectionDelegate = builder.Build();

            options.EndPoint = await BindAsync(options, multiplexedConnectionDelegate, cancellationToken).ConfigureAwait(false);
        }
    }
}