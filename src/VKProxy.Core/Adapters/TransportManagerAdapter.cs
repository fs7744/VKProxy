using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using VKProxy.Core.Config;

namespace VKProxy.Core.Adapters;

public class TransportManagerAdapter
{
    private static MethodInfo StopAsyncMethod;
    private static MethodInfo StopEndpointsAsyncMethod;
    private static MethodInfo MultiplexedBindAsyncMethod;
    private static MethodInfo BindAsyncMethod;
    private object transportManager;
    private object heartbeat;
    private static MethodInfo StartHeartbeatMethod;

    public TransportManagerAdapter(IServiceProvider serviceProvider)
    {
        (transportManager, heartbeat) = CreateTransportManager(serviceProvider);
    }

    private static (object, object) CreateTransportManager(IServiceProvider serviceProvider)
    {
        var types = typeof(KestrelServer).Assembly.GetTypes();
        var TransportManagerType = types.First(i => i.Name == "TransportManager");

        foreach (var item in TransportManagerType.GetTypeInfo().DeclaredMethods)
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

        var s = CreateServiceContext(serviceProvider, types);
        var r = Activator.CreateInstance(TransportManagerType,
                    Enumerable.Reverse(serviceProvider.GetServices<IConnectionListenerFactory>()).ToList(),
                    Enumerable.Reverse(serviceProvider.GetServices<IMultiplexedConnectionListenerFactory>()).ToList(),
                    CreateHttpsConfigurationService(serviceProvider, types),
                    s.context
                    );
        return (r, s.heartbeat);

        static object CreateHttpsConfigurationService(IServiceProvider serviceProvider, Type[] types)
        {
            var HttpsConfigurationServiceType = types.First(i => i.Name == "HttpsConfigurationService");
            var HttpsConnectionMiddlewareType = types.First(i => i.Name == "HttpsConnectionMiddleware");
            var CreateLogger = typeof(LoggerFactoryExtensions).GetTypeInfo().DeclaredMethods.First(i => i.Name == "CreateLogger" && i.ContainsGenericParameters);
            var r = Activator.CreateInstance(HttpsConfigurationServiceType);
            var m = HttpsConfigurationServiceType.GetMethod("Initialize");
            var log = serviceProvider.GetRequiredService<ILoggerFactory>();
            var l = CreateLogger.MakeGenericMethod(HttpsConnectionMiddlewareType).Invoke(null, new object[] { log });
            m.Invoke(r, new object[] { serviceProvider.GetRequiredService<IHostEnvironment>(), log.CreateLogger<KestrelServer>(), l });
            return r;
        }

        static (object context, object heartbeat) CreateServiceContext(IServiceProvider serviceProvider, Type[] types)
        {
            var KestrelServerImplType = types.First(i => i.Name == "KestrelServerImpl");
            var KestrelCreateServiceContext = KestrelServerImplType.GetMethod("CreateServiceContext", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var r = KestrelCreateServiceContext.Invoke(null, new object[]
            {
                serviceProvider.GetRequiredService<IOptions<KestrelServerOptions>>(),
                serviceProvider.GetRequiredService<ILoggerFactory>(),
                null,
                CreateKestrelMetrics(types)
            });
            var ServiceContextType = types.First(i => i.Name == "ServiceContext");
            var h = ServiceContextType.GetTypeInfo().DeclaredProperties.First(i => i.Name == "Heartbeat");

            var HeartbeatType = types.First(i => i.Name == "Heartbeat");
            StartHeartbeatMethod = HeartbeatType.GetTypeInfo().DeclaredMethods.First(i => i.Name == "Start");
            return (r, h.GetGetMethod().Invoke(r, null));
        }

        static object CreateKestrelMetrics(Type[] types)
        {
            var KestrelMetricsType = types.First(i => i.Name == "KestrelMetrics");
            var DummyMeterFactoryType = types.First(i => i.Name == "DummyMeterFactory");
            return Activator.CreateInstance(KestrelMetricsType, Activator.CreateInstance(DummyMeterFactoryType));
        }
    }

    public Task<EndPoint> BindAsync(EndPoint endPoint, ConnectionDelegate connectionDelegate, EndPointOptions? endpointConfig, CancellationToken cancellationToken)
    {
        return BindAsyncMethod.Invoke(transportManager, new object[] { endPoint, connectionDelegate, endpointConfig?.Init(), cancellationToken }) as Task<EndPoint>;
    }

    public Task<EndPoint> BindAsync(EndPoint endPoint, MultiplexedConnectionDelegate multiplexedConnectionDelegate, EndPointOptions? endpointConfig, CancellationToken cancellationToken)
    {
        return MultiplexedBindAsyncMethod.Invoke(transportManager, new object[] { endPoint, multiplexedConnectionDelegate, endpointConfig?.Init(), cancellationToken }) as Task<EndPoint>;
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
}