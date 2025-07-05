using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using VKProxy.Config;
using VKProxy.Core.Buffers;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Features;
using VKProxy.LoadBalancing;

namespace VKProxy.Middlewares;

public interface ITcpReverseProxy
{
    Task Proxy(ConnectionContext context, IL4ReverseProxyFeature feature);
}

internal class TcpReverseProxy : ITcpReverseProxy
{
    private readonly IConnectionFactory tcp;
    private readonly ProxyLogger logger;
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    private readonly ISniSelector sniSelector;
    private readonly ReverseProxyOptions options;
    private readonly TcpProxyDelegate req;
    private readonly TcpProxyDelegate resp;
    private readonly TcpDelegate init;

    public TcpReverseProxy(IConnectionFactory tcp, ProxyLogger logger, ILoadBalancingPolicyFactory loadBalancing, IEnumerable<ITcpProxyMiddleware> middlewares,
        IOptions<ReverseProxyOptions> options, ISniSelector sniSelector)
    {
        this.tcp = tcp;
        this.logger = logger;
        this.loadBalancing = loadBalancing;
        this.sniSelector = sniSelector;
        this.options = options.Value;
        (init, req, resp) = BuildMiddlewares(middlewares);
    }

    private (TcpDelegate init, TcpProxyDelegate req, TcpProxyDelegate resp) BuildMiddlewares(IEnumerable<ITcpProxyMiddleware> middlewares)
    {
        TcpDelegate init = (context, c) => Task.CompletedTask;
        TcpProxyDelegate req = (context, s, c) => Task.FromResult(s);
        TcpProxyDelegate resp = (context, s, c) => Task.FromResult(s);

        foreach (var middleware in middlewares)
        {
            Func<TcpDelegate, TcpDelegate> m = (next) => (c, t) => middleware.InitAsync(c, t, next);
            init = m(init);

            Func<TcpProxyDelegate, TcpProxyDelegate> r = (next) => (c, s, t) => middleware.OnRequestAsync(c, s, t, next);
            req = r(req);

            r = (next) => (c, s, t) => middleware.OnResponseAsync(c, s, t, next);
            resp = r(resp);
        }

        return (init, req, resp);
    }

    public async Task Proxy(ConnectionContext context, IL4ReverseProxyFeature feature)
    {
        if (feature.IsSni)
        {
            await SniProxyAsync(context, feature);
        }
        else
        {
            await TcpProxyAsync(context, feature);
        }
    }

    private async Task TcpProxyAsync(ConnectionContext context, IL4ReverseProxyFeature feature)
    {
        var route = feature.Route;
        if (route is null) return;
        logger.ProxyBegin(route.Key);
        await DoTcpProxyAsync(context, feature, route);
        logger.ProxyEnd(route.Key);
    }

    private async Task DoTcpProxyAsync(ConnectionContext context, IL4ReverseProxyFeature feature, RouteConfig? route)
    {
        ConnectionContext upstream = null;
        try
        {
            using var cts = CancellationTokenSourcePool.Default.Rent(route.Timeout);
            var token = cts.Token;
            await init(context, token);
            if (feature.IsDone) return;
            upstream = await DoConnectionAsync(feature, route);
            if (upstream is null)
            {
                logger.NotFoundAvailableUpstream(route.ClusterId);
            }
            else
            {
                feature.SelectedDestination?.ConcurrencyCounter.Increment();
                var task = await Task.WhenAny(
                        context.Transport.Input.CopyToAsync(new MiddlewarePipeWriter(upstream.Transport.Output, context, req), token)
                        , upstream.Transport.Input.CopyToAsync(new MiddlewarePipeWriter(context.Transport.Output, context, resp), token));
                if (task.IsCanceled)
                {
                    logger.ProxyTimeout(route.Key, route.Timeout);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.ConnectUpstreamTimeout(route.Key);
        }
        catch (Exception ex)
        {
            logger.UnexpectedException(nameof(TcpReverseProxy), ex);
        }
        finally
        {
            feature.SelectedDestination?.ConcurrencyCounter.Decrement();
            upstream?.Abort();
        }
    }

    private async Task SniProxyAsync(ConnectionContext context, IL4ReverseProxyFeature feature)
    {
        using var cts = CancellationTokenSourcePool.Default.Rent(options.ConnectionTimeout);
        var token = cts.Token;
        await init(context, token);
        if (feature.IsDone) return;
        var (sni, r) = await sniSelector.MatchSNIAsync(context, token);
        if (sni is null) return;
        var route = sni.RouteConfig;
        if (route is null) return;
        feature.Route = route;
        logger.ProxyBegin(route.Key);
        if (sni.Passthrough)
        {
            await DoPassthroughAsync(context, route, r, feature);
        }
        else
        {
            await DoSslAsync(context, sni, r, feature);
        }

        logger.ProxyEnd(route.Key);
    }

    private async Task DoPassthroughAsync(ConnectionContext context, RouteConfig? route, ReadResult r, IL4ReverseProxyFeature feature)
    {
        ConnectionContext upstream = null;
        try
        {
            upstream = await DoConnectionAsync(feature, route);
            if (upstream is null)
            {
                logger.NotFoundAvailableUpstream(route.ClusterId);
            }
            else
            {
                feature.SelectedDestination?.ConcurrencyCounter.Increment();
                using var cts = CancellationTokenSourcePool.Default.Rent(route.Timeout);
                var t = cts.Token;
                await r.CopyToAsync(upstream.Transport.Output, t);
                context.Transport.Input.AdvanceTo(r.Buffer.End);
                var task = await Task.WhenAny(
                        context.Transport.Input.CopyToAsync(new MiddlewarePipeWriter(upstream.Transport.Output, context, req), t)
                        , upstream.Transport.Input.CopyToAsync(new MiddlewarePipeWriter(context.Transport.Output, context, resp), t));
                if (task.IsCanceled)
                {
                    logger.ProxyTimeout(route.Key, route.Timeout);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.ConnectUpstreamTimeout(route.Key);
        }
        catch (Exception ex)
        {
            logger.UnexpectedException(nameof(DoPassthroughAsync), ex);
        }
        finally
        {
            feature.SelectedDestination?.ConcurrencyCounter.Decrement();
            upstream?.Abort();
        }
    }

    private async Task DoSslAsync(ConnectionContext context, SniConfig sni, ReadResult r, IL4ReverseProxyFeature feature)
    {
        var sslDuplexPipe = CreateSslDuplexPipe(r, context.Transport, context is IMemoryPoolFeature s ? s.MemoryPool : MemoryPool<byte>.Shared, SslStreamFactory);
        var sslStream = sslDuplexPipe.Stream;
        context.Transport = sslDuplexPipe;
        using var cts = CancellationTokenSourcePool.Default.Rent(sni.HandshakeTimeout);
        await sslStream.AuthenticateAsServerAsync(sni.GenerateOptions(), cts.Token);
        await TcpProxyAsync(context, feature);
    }

    private SslStream SslStreamFactory(Stream stream) => new SslStream(stream);

    private SslDuplexPipe CreateSslDuplexPipe(ReadResult readResult, IDuplexPipe transport, MemoryPool<byte> memoryPool, Func<Stream, SslStream> sslStreamFactory)
    {
        StreamPipeReaderOptions inputPipeOptions = new StreamPipeReaderOptions
        (
            pool: memoryPool,
            bufferSize: memoryPool.GetMinimumSegmentSize(),
            minimumReadSize: memoryPool.GetMinimumAllocSize(),
            leaveOpen: true,
            useZeroByteReads: true
        );

        var outputPipeOptions = new StreamPipeWriterOptions
        (
            pool: memoryPool,
            leaveOpen: true
        );

        return new SslDuplexPipe(readResult, transport, inputPipeOptions, outputPipeOptions, sslStreamFactory);
    }

    private async Task<ConnectionContext> DoConnectionAsync(IL4ReverseProxyFeature feature, RouteConfig route)
    {
        DestinationState selectedDestination = null;
        try
        {
            feature.SelectedDestination = selectedDestination = loadBalancing.PickDestination(feature);
            if (selectedDestination is null)
            {
                return null;
            }
            using var cts = CancellationTokenSourcePool.Default.Rent(options.ConnectionTimeout);
            var c = await tcp.ConnectAsync(selectedDestination.EndPoint, cts.Token);
            selectedDestination.ReportSuccessed();
            return c;
        }
        catch
        {
            selectedDestination?.ReportFailed();
            throw;
        }
    }
}