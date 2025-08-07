using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.RateLimiting;
using VKProxy.Config;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Http;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Features;
using VKProxy.Middlewares;

namespace VKProxy;

internal class ListenHandler : ListenHandlerBase
{
    internal const string ActivityName = "VKProxy.ReverseProxy";
    private readonly IConfigSource<IProxyConfig> configSource;
    private readonly ProxyLogger logger;
    private readonly IHttpSelector httpSelector;
    private readonly ISniSelector sniSelector;
    private readonly IUdpReverseProxy udp;
    private readonly ITcpReverseProxy tcp;
    private readonly IServiceProvider provider;
    private readonly DistributedContextPropagator propagator;
    private readonly ActivitySource activitySource;
    private readonly ReverseProxyOptions options;
    private readonly RequestDelegate http;

    public ListenHandler(IConfigSource<IProxyConfig> configSource, ProxyLogger logger, IHttpSelector httpSelector,
        ISniSelector sniSelector, IUdpReverseProxy udp, ITcpReverseProxy tcp, IApplicationBuilder applicationBuilder, IServiceProvider provider, IOptions<ReverseProxyOptions> options,
        DistributedContextPropagator propagator, ActivitySource activitySource)
    {
        this.configSource = configSource;
        this.logger = logger;
        this.httpSelector = httpSelector;
        this.sniSelector = sniSelector;
        this.udp = udp;
        this.tcp = tcp;
        this.provider = provider;
        this.propagator = propagator;
        this.activitySource = activitySource;
        this.options = options.Value;
        this.http = applicationBuilder.Build();
    }

    public override Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        return BindDiffAsync(transportManager, cancellationToken);
    }

    public override IChangeToken? GetReloadToken()
    {
        return configSource.GetChangeToken();
    }

    public override Task RebindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        return BindDiffAsync(transportManager, cancellationToken);
    }

    public override Task StopAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        configSource.Dispose();
        var disk = provider.GetService<IDiskCache>();
        if (disk != null)
        {
            disk.Dispose();
        }
        return Task.CompletedTask;
    }

    private async Task BindDiffAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        var (stop, start) = await configSource.GenerateDiffAsync(cancellationToken);
        if (stop != null)
        {
            var c = new CancellationTokenSource();
            c.CancelAfter(1000);
            await transportManager.StopEndpointsAsync(stop.ToList<EndPointOptions>(), c.Token);
        }

        if (start != null)
        {
            foreach (var s in start)
            {
                try
                {
                    await OnBindAsync(transportManager, s, cancellationToken).ConfigureAwait(false);
                    logger.BindListenOptions(s);
                }
                catch (Exception ex)
                {
                    logger.BindListenOptionsError(s, ex);
                }
            }
        }
    }

    private async Task OnBindAsync(ITransportManager transportManager, ListenEndPointOptions options, CancellationToken cancellationToken)
    {
        if (options.Protocols == GatewayProtocols.UDP)
        {
            await transportManager.BindAsync(options, c => DoUdp(c, options), cancellationToken);
        }
        else if (options.Protocols == GatewayProtocols.TCP)
        {
            await transportManager.BindAsync(options, c => DoTcp(c, options), cancellationToken);
        }
        else
        {
            var https = options.GetHttpsOptions();
            if (https != null && https.ServerCertificate == null && https.ServerCertificateSelector == null)
            {
                https.ServerCertificateSelector = sniSelector.ServerCertificateSelector;
            }
            await transportManager.BindHttpAsync(options, c => DoHttp(c, options), cancellationToken, options.GetHttpProtocols(), true, null, null, https);
        }
    }

    private async Task DoHttp(HttpContext context, ListenEndPointOptions? options)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        //var diagnosticListenerEnabled = diagnosticSource.IsEnabled();
        //var diagnosticListenerActivityCreationEnabled = (diagnosticListenerEnabled && diagnosticSource.IsEnabled(ActivityName, context));
        Activity activity;
        if (activitySource.HasListeners())
        {
            var headers = context.Request.Headers;
            activity = ActivityCreator.CreateFromRemote(activitySource, propagator, headers,
                static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                fieldValues = default;
                var headers = (IHeaderDictionary)carrier!;
                fieldValue = headers[fieldName];
            },
            ActivityName,
            ActivityKind.Server,
            tags: null,
            links: null, false);
        }
        else
        {
            activity = null;
        }

        if (activity != null)
        {
            activity.Start();
            context.Features.Set<IHttpActivityFeature>(new HttpActivityFeature(activity));
            context.Features.Set<IHttpMetricsTagsFeature>(new HttpMetricsTagsFeature()
            {
                Method = context.Request.Method,
                Protocol = context.Request.Protocol,
                Scheme = context.Request.Scheme,
                MetricsDisabled = true,
            });
        }

        using var proxyFeature = new L7ReverseProxyFeature() { Http = context, StartTimestamp = startTimestamp };
        context.Features.Set<IReverseProxyFeature>(proxyFeature);
        try
        {
            proxyFeature.Route = options?.RouteConfig ?? await httpSelector.MatchAsync(context);
            logger.ProxyBegin(proxyFeature);
            var limiter = proxyFeature.Route?.ConnectionLimiter?.GetLimiter(proxyFeature);
            if (limiter == null)
                await http(context);
            else
            {
                var activityCancellationSource = ActivityCancellationTokenSource.Rent(proxyFeature.Route?.Timeout ?? this.options.ConnectionTimeout, context.RequestAborted);
                using RateLimitLease lease = await limiter.AcquireAsync(permitCount: 1, activityCancellationSource.Token);
                if (lease.IsAcquired)
                {
                    await http(context);
                    return;
                }

                logger.ConnectionRejected(context.Connection.Id);
                if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }
                context.Response.StatusCode = 429;
            }
        }
        finally
        {
            activity?.Stop();
            logger.ProxyEnd(proxyFeature);
        }
    }

    private async Task DoTcp(ConnectionContext connection, ListenEndPointOptions? options)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        using var proxyFeature = new L4ReverseProxyFeature() { Route = options?.RouteConfig, IsSni = options?.UseSni ?? false, SelectedSni = options?.SniConfig, Connection = connection, StartTimestamp = startTimestamp };
        connection.Features.Set<IL4ReverseProxyFeature>(proxyFeature);
        try
        {
            var limiter = proxyFeature.Route?.ConnectionLimiter?.GetLimiter(proxyFeature);
            if (limiter == null)
                await tcp.Proxy(connection, proxyFeature);
            else
            {
                using var s = CancellationTokenSourcePool.Default.Rent(proxyFeature.Route?.Timeout ?? this.options.ConnectionTimeout);
                using RateLimitLease lease = await limiter.AcquireAsync(permitCount: 1, s.Token);
                if (lease.IsAcquired)
                {
                    await tcp.Proxy(connection, proxyFeature);
                    return;
                }

                logger.ConnectionRejected(connection.ConnectionId);
                await connection.DisposeAsync();
            }
        }
        finally
        {
            logger.ProxyEnd(proxyFeature);
        }
    }

    private async Task DoUdp(ConnectionContext connection, ListenEndPointOptions? options)
    {
        if (connection is UdpConnectionContext context)
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            using var proxyFeature = new L4ReverseProxyFeature() { Route = options?.RouteConfig, Connection = connection, StartTimestamp = startTimestamp };
            context.Features.Set<IL4ReverseProxyFeature>(proxyFeature);
            logger.ProxyBegin(proxyFeature);
            try
            {
                var limiter = proxyFeature.Route?.ConnectionLimiter?.GetLimiter(proxyFeature);
                if (limiter == null)
                    await udp.Proxy(context, proxyFeature);
                else
                {
                    using var s = CancellationTokenSourcePool.Default.Rent(proxyFeature.Route?.Timeout ?? this.options.ConnectionTimeout);
                    using RateLimitLease lease = await limiter.AcquireAsync(permitCount: 1, s.Token);
                    if (lease.IsAcquired)
                    {
                        await udp.Proxy(context, proxyFeature);
                        return;
                    }

                    logger.ConnectionRejected(connection.ConnectionId);
                    await connection.DisposeAsync();
                }
            }
            finally
            {
                logger.ProxyEnd(proxyFeature);
            }
        }
        else
            await connection.DisposeAsync();
    }
}