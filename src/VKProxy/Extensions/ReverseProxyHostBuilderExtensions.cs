using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Net.Quic;
using VKProxy;
using VKProxy.Config;
using VKProxy.Config.Validators;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Health;
using VKProxy.Health.ActiveHealthCheckers;
using VKProxy.LoadBalancing;
using VKProxy.Middlewares;
using VKProxy.Middlewares.Http;
using VKProxy.ServiceDiscovery;

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
            services.AddSingleton<IValidator<ClusterConfig>, ClusterConfigValidator>();
            services.AddSingleton<IEndPointConvertor, CommonEndPointConvertor>();
            services.AddSingleton<ISniSelector, SniSelector>();
            services.AddSingleton<IHttpSelector, HttpSelector>();
            services.AddSingleton<IDestinationResolver, DnsDestinationResolver>();

            services.AddSingleton<ILoadBalancingPolicy, RandomLoadBalancingPolicy>();
            services.AddSingleton<ILoadBalancingPolicy, RoundRobinLoadBalancingPolicy>();
            services.AddSingleton<ILoadBalancingPolicy, LeastRequestsLoadBalancingPolicy>();
            services.AddSingleton<ILoadBalancingPolicy, PowerOfTwoChoicesLoadBalancingPolicy>();
            services.AddSingleton<ILoadBalancingPolicyFactory, LoadBalancingPolicy>();

            services.AddSingleton<IHealthReporter, PassiveHealthReporter>();
            services.AddSingleton<IHealthUpdater, HealthyAndUnknownDestinationsUpdater>();
            services.AddSingleton<IActiveHealthCheckMonitor, ActiveHealthCheckMonitor>();
            services.AddSingleton<IActiveHealthChecker, ConnectionActiveHealthChecker>();
            services.AddSingleton(TimeProvider.System);

            services.AddSingleton<IUdpReverseProxy, UdpReverseProxy>();
            services.AddSingleton<ITcpReverseProxy, TcpReverseProxy>();
            services.AddSingleton<HttpReverseProxy>();
            services.AddScoped<IMiddlewareFactory, MiddlewareFactory>();
            services.AddSingleton<IApplicationBuilder>(i =>
            {
                var app = new ApplicationBuilder(i);
                foreach (var item in i.GetServices<Action<IApplicationBuilder>>())
                {
                    item(app);
                }
                app.Use(i.GetRequiredService<HttpReverseProxy>().InvokeAsync);
                return app;
            });
        });

        return hostBuilder;
    }

    public static IServiceCollection UseUdpMiddleware<T>(this IServiceCollection services) where T : class, IUdpProxyMiddleware
    {
        services.AddTransient<IUdpProxyMiddleware, T>();
        return services;
    }

    public static IServiceCollection UseTcpMiddleware<T>(this IServiceCollection services) where T : class, ITcpProxyMiddleware
    {
        services.AddTransient<ITcpProxyMiddleware, T>();
        return services;
    }

    public static IServiceCollection UseHttpMiddleware<T>(this IServiceCollection services, params object?[] args) where T : class
    {
        if (typeof(IMiddleware).IsAssignableFrom(typeof(T)))
        {
            services.TryAddSingleton<T>();
        }
        services.ConfigeHttp(i => i.UseMiddleware<T>());
        return services;
    }

    public static IServiceCollection UseHttpMiddleware(this IServiceCollection services, Func<RequestDelegate, RequestDelegate> middleware)
    {
        services.ConfigeHttp(i => i.Use(middleware));
        return services;
    }

    public static IServiceCollection UseHttpMiddleware(this IServiceCollection services, Func<HttpContext, RequestDelegate, Task> middleware)
    {
        services.ConfigeHttp(i => i.Use(middleware));
        return services;
    }

    public static IServiceCollection ConfigeHttp(this IServiceCollection services, Action<IApplicationBuilder> configeHttp)
    {
        services.AddSingleton<Action<IApplicationBuilder>>(configeHttp);
        return services;
    }
}