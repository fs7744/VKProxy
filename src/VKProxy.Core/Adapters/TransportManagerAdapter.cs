using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using VKProxy.Core.Config;

namespace VKProxy.Core.Adapters;

public class TransportManagerAdapter : ITransportManager, IHeartbeat, IHttpServerBuilder
{
    private static MethodInfo StopAsyncMethod;
    private static MethodInfo StopEndpointsAsyncMethod;
    private static MethodInfo MultiplexedBindAsyncMethod;
    private static MethodInfo BindAsyncMethod;
    private static MethodInfo StartHeartbeatMethod;
    private object transportManager;
    private object heartbeat;
    private object serviceContext;

    public TransportManagerAdapter(IServiceProvider serviceProvider)
    {
        (transportManager, heartbeat, serviceContext) = CreateTransportManager(serviceProvider);
    }

    private static (object, object, object) CreateTransportManager(IServiceProvider serviceProvider)
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
        return (r, s.heartbeat, s.context);

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

        static (object context, object heartbeat) CreateServiceContext(IServiceProvider serviceProvider)
        {
            var KestrelCreateServiceContext = KestrelExtensions.KestrelServerImplType.GetMethod("CreateServiceContext", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var r = KestrelCreateServiceContext.Invoke(null, new object[]
            {
                serviceProvider.GetRequiredService<IOptions<KestrelServerOptions>>(),
                serviceProvider.GetRequiredService<ILoggerFactory>(),
                null,
                CreateKestrelMetrics()
            });
            var h = KestrelExtensions.ServiceContextType.GetTypeInfo().DeclaredProperties.First(i => i.Name == "Heartbeat");

            StartHeartbeatMethod = KestrelExtensions.HeartbeatType.GetTypeInfo().DeclaredMethods.First(i => i.Name == "Start");
            return (r, h.GetGetMethod().Invoke(r, null));
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
        return MultiplexedBindAsyncMethod.Invoke(transportManager, new object[] { endpointConfig.EndPoint, multiplexedConnectionDelegate, endpointConfig.Init(), cancellationToken }) as Task<EndPoint>;
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
}