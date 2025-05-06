using Microsoft.AspNetCore.Connections;
using Microsoft.Net.Http.Headers;
using System.Collections.Frozen;
using System.Net;
using VKProxy.Config;
using VKProxy.Core.Http;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Features;
using VKProxy.LoadBalancing;
using VKProxy.Middlewares.Http;
using VKProxy.Middlewares.Http.Transforms;

namespace VKProxy.Middlewares.Socks5;

internal class Socks5ToWSMiddleware : ITcpProxyMiddleware
{
    private readonly FrozenDictionary<byte, ISocks5Auth> auths;
    private readonly IForwarderHttpClientFactory httpClientFactory;
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    private readonly ProxyLogger logger;
    private readonly TimeProvider timeProvider;

    public Socks5ToWSMiddleware(IEnumerable<ISocks5Auth> socks5Auths, IForwarderHttpClientFactory httpClientFactory, ILoadBalancingPolicyFactory loadBalancing, ProxyLogger logger, TimeProvider timeProvider)
    {
        this.auths = socks5Auths.ToFrozenDictionary(i => i.AuthType);
        this.httpClientFactory = httpClientFactory;
        this.loadBalancing = loadBalancing;
        this.logger = logger;
        this.timeProvider = timeProvider;
    }

    public Task InitAsync(ConnectionContext context, CancellationToken token, TcpDelegate next)
    {
        var feature = context.Features.Get<IL4ReverseProxyFeature>();
        if (feature is not null)
        {
            var route = feature.Route;
            if (route is not null && route.Metadata is not null
                && route.Metadata.TryGetValue("socks5ToWS", out var b) && bool.TryParse(b, out var isSocks5) && isSocks5)
            {
                feature.IsDone = true;
                route.ClusterConfig?.InitHttp(httpClientFactory);
                return Proxy(context, feature, token);
            }
        }
        return next(context, token);
    }

    private async Task Proxy(ConnectionContext context, IL4ReverseProxyFeature feature, CancellationToken token)
    {
        var route = feature.Route;
        var cluster = route.ClusterConfig;
        DestinationState selectedDestination;
        if (cluster is null)
        {
            selectedDestination = null;
        }
        else
        {
            selectedDestination = feature.SelectedDestination;
            selectedDestination ??= loadBalancing.PickDestination(feature);
        }

        if (selectedDestination is null)
        {
            logger.NotFoundAvailableUpstream(route.ClusterId);
            Abort(context);
            return;
        }
        selectedDestination.ConcurrencyCounter.Increment();
        try
        {
            await SendAsync(context, feature, selectedDestination, cluster, route.Transformer, token);
            selectedDestination.ReportSuccessed();
        }
        catch
        {
            selectedDestination.ReportFailed();
            throw;
        }
        finally
        {
            selectedDestination.ConcurrencyCounter.Decrement();
        }
    }

    private async Task<ForwarderError> SendAsync(ConnectionContext context, IL4ReverseProxyFeature feature, DestinationState selectedDestination, ClusterConfig? cluster, IHttpTransformer transformer, CancellationToken token)
    {
        var destinationPrefix = selectedDestination.Address;
        // "http://a".Length = 8
        if (destinationPrefix is null || destinationPrefix.Length < 8)
        {
            throw new ArgumentException("Invalid destination prefix.", nameof(destinationPrefix));
        }
        var route = feature.Route;
        var requestConfig = cluster.HttpRequest ?? ForwarderRequestConfig.Empty;
        var httpClient = cluster.HttpMessageHandler ?? throw new ArgumentNullException("httpClient");
        var destinationRequest = new HttpRequestMessage();
        destinationRequest.Version = HttpVersion.Version11;
        destinationRequest.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        destinationRequest.Method = HttpMethod.Get;
        destinationRequest.RequestUri ??= new Uri(destinationPrefix, UriKind.Absolute);
        destinationRequest.Headers.TryAddWithoutValidation(HeaderNames.Connection, HeaderNames.Upgrade);
        destinationRequest.Headers.TryAddWithoutValidation(HeaderNames.Upgrade, HttpForwarder.WebSocketName);
        destinationRequest.Headers.TryAddWithoutValidation(HeaderNames.SecWebSocketVersion, "13");
        destinationRequest.Headers.TryAddWithoutValidation(HeaderNames.SecWebSocketKey, ProtocolHelper.CreateSecWebSocketKey());
        destinationRequest.Content = new EmptyHttpContent();

        var destinationResponse = await httpClient.SendAsync(destinationRequest, token);
        if (destinationResponse.StatusCode == HttpStatusCode.SwitchingProtocols)
        {
            using var destinationStream = await destinationResponse.Content.ReadAsStreamAsync(token);
            using var clientStream = new DuplexPipeStreamAdapter<Stream>(null, context.Transport, static i => i);
            var activityCancellationSource = ActivityCancellationTokenSource.Rent(route.Timeout);
            var requestTask = StreamCopier.CopyAsync(isRequest: true, clientStream, destinationStream, StreamCopier.UnknownLength, timeProvider, activityCancellationSource,
                // HTTP/2 HttpClient request streams buffer by default.
                autoFlush: destinationResponse.Version == HttpVersion.Version20, token).AsTask();
            var responseTask = StreamCopier.CopyAsync(isRequest: false, destinationStream, clientStream, StreamCopier.UnknownLength, timeProvider, activityCancellationSource, token).AsTask();

            var task = await Task.WhenAny(requestTask, responseTask);
            if (task.IsCanceled)
            {
                Abort(context);
                activityCancellationSource.Cancel();
                if (task.Exception is not null)
                {
                    throw task.Exception;
                }
            }
        }
        else
        {
            Abort(context);
            return ForwarderError.UpgradeRequestDestination;
        }

        return ForwarderError.None;
    }

    public Task<ReadOnlyMemory<byte>> OnRequestAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        return next(context, source, token);
    }

    public Task<ReadOnlyMemory<byte>> OnResponseAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        return next(context, source, token);
    }

    private static void Abort(ConnectionContext upstream)
    {
        upstream.Transport.Input.CancelPendingRead();
        upstream.Transport.Output.CancelPendingFlush();
        upstream.Abort();
    }
}