using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Net.Quic;
using VKProxy;
using VKProxy.Config;
using VKProxy.Config.Validators;
using VKProxy.Core.Adapters;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Features.Limits;
using VKProxy.Health;
using VKProxy.Health.ActiveHealthCheckers;
using VKProxy.LoadBalancing;
using VKProxy.LoadBalancing.SessionAffinity;
using VKProxy.Middlewares;
using VKProxy.Middlewares.Http;
using VKProxy.Middlewares.Http.HttpFuncs;
using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;
using VKProxy.Middlewares.Http.Transforms;
using VKProxy.Middlewares.Socks5;
using VKProxy.ServiceDiscovery;
using VKProxy.TemplateStatement;

namespace Microsoft.Extensions.Hosting;

public static class ReverseProxyHostBuilderExtensions
{
    public static IHostBuilder UseReverseProxy(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseVKProxyCore();
        hostBuilder.ConfigureServices(ConfigReverseProxy);
        return hostBuilder;
    }

    public static IHostApplicationBuilder UseReverseProxy(this IHostApplicationBuilder hostBuilder)
    {
        hostBuilder.UseVKProxyCore();
        ConfigReverseProxy(hostBuilder.Services);
        return hostBuilder;
    }

    public static IServiceCollection UseReverseProxy(this IServiceCollection services)
    {
        services.UseInternalKestrelCore();
        services.UseVKProxyCore();
        ConfigReverseProxy(services);

        return services;
    }

    private static void ConfigReverseProxy(IServiceCollection services)
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
        services.AddSingleton<IValidator<RouteConfig>, RouteConfigValidator>();
        services.AddSingleton<IEndPointConvertor, CommonEndPointConvertor>();
        services.AddSingleton<ISniSelector, SniSelector>();
        services.AddSingleton<IHttpSelector, HttpSelector>();
        services.AddSingleton<IHostResolver, DnsDestinationResolver>();
        services.AddSingleton<IDestinationResolver>(i => i.GetRequiredService<IHostResolver>() as IDestinationResolver);
        services.AddSingleton<IDestinationConfigParser, DestinationConfigParser>();

        services.AddSingleton<ILoadBalancingPolicy, RandomLoadBalancingPolicy>();
        services.AddSingleton<ILoadBalancingPolicy, RoundRobinLoadBalancingPolicy>();
        services.AddSingleton<ILoadBalancingPolicy, LeastRequestsLoadBalancingPolicy>();
        services.AddSingleton<ILoadBalancingPolicy, PowerOfTwoChoicesLoadBalancingPolicy>();
        services.AddSingleton<ILoadBalancingPolicy, HashLoadBalancingPolicy>();
        services.AddSingleton<ILoadBalancingPolicyFactory, LoadBalancingPolicy>();

        services.AddDataProtection();
        services.AddSingleton<SessionAffinityLoadBalancingPolicy>();

        services.AddSingleton<IHealthReporter, PassiveHealthReporter>();
        services.AddSingleton<IHealthUpdater, HealthyAndUnknownDestinationsUpdater>();
        services.AddSingleton<IActiveHealthCheckMonitor, ActiveHealthCheckMonitor>();
        services.AddSingleton<IActiveHealthChecker, ConnectionActiveHealthChecker>();
        services.AddSingleton<IActiveHealthChecker, HttpActiveHealthChecker>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IUdpReverseProxy, UdpReverseProxy>();
        services.AddSingleton<ITcpReverseProxy, TcpReverseProxy>();
        services.AddSingleton<HttpReverseProxy>();
        services.AddSingleton<IHttpForwarder, HttpForwarder>();
        services.AddSingleton<IForwarderHttpClientFactory, ForwarderHttpClientFactory>();

        services.AddSingleton<ITransformBuilder, TransformBuilder>();
        services.AddSingleton<ITransformFactory, ForwardedTransformFactory>();
        services.AddSingleton<ITransformFactory, HttpMethodTransformFactory>();
        services.AddSingleton<ITransformFactory, PathTransformFactory>();
        services.AddSingleton<ITransformFactory, QueryTransformFactory>();
        services.AddSingleton<ITransformFactory, RequestHeadersTransformFactory>();
        services.AddSingleton<ITransformFactory, ResponseTransformFactory>();

        services.AddSingleton<IConnectionLimitFactory, ConnectionLimitFactory>();
        services.AddSingleton<IConnectionLimitCreator, ConnectionLimitByTotalCreator>();
        services.AddSingleton<IConnectionLimitCreator, ConnectionLimitByKeyCreator>();

        services.AddSingleton<IHttpFunc, CorsFunc>();
        services.AddSingleton<ITransformProvider, CorsResponseHeaderRemoveTransform>();

        services.AddSingleton<IHttpFunc, ResponseCachingFunc>();
        services.AddSingleton<IResponseCache, MemoryResponseCache>();

        services.AddSingleton<IHttpFunc, ResponseCompressionFunc>();
        services.AddSingleton<IHttpFunc, MirrorFunc>();
        services.AddSingleton<IHttpFunc, WAFFunc>();
        services.AddSingleton<IHttpFunc, OnlyHttpsFunc>();
        services.AddSingleton<IHttpFunc, ContentFunc>();

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton<ITemplateStatementFactory, TemplateStatementFactory>();

        services.AddScoped<IMiddlewareFactory, MiddlewareFactory>();
        services.AddSingleton<IApplicationBuilder>(i =>
        {
            var app = new ApplicationBuilder(i);
#if DEBUG
            app.Use(async (c, next) =>
            {
                var req = c.Request;
                var path = req.Path.ToString();
                var host = req.Host.ToString();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    await next(c);
                }
                finally
                {
                    sw.Stop();
                    c.RequestServices.GetRequiredService<ILogger<HttpReverseProxy>>().LogInformation($"{req.Protocol} {host} {path} end used: {sw.Elapsed}");
                }
            });
#endif
            foreach (var item in i.GetServices<Action<IApplicationBuilder>>())
            {
                item(app);
            }
            app.Use(i.GetRequiredService<HttpReverseProxy>().InvokeAsync);
            return app;
        });
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

    public static IServiceCollection UseSocks5(this IServiceCollection services)
    {
        services.AddSingleton<ISocks5Auth, Socks5NoAuth>();
        services.AddSingleton<ISocks5Auth, Socks5PasswordAuth>();
        services.AddSingleton<Socks5Middleware>();
        services.AddTransient<ITcpProxyMiddleware>(i => i.GetRequiredService<Socks5Middleware>());
        services.AddTransient<ITcpProxyMiddleware, Socks5ToWSMiddleware>();
        return services;
    }

    public static IServiceCollection UseWSToSocks5(this IServiceCollection services)
    {
        return services.UseHttpMiddleware<WSToSocks5HttpMiddleware>();
    }

    public static IServiceCollection UseHttpMiddleware<T>(this IServiceCollection services, params object?[] args) where T : class
    {
        if (typeof(IMiddleware).IsAssignableFrom(typeof(T)))
        {
            services.TryAddSingleton<T>();
        }
        services.ConfigeHttp(i => i.UseMiddleware<T>(args));
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