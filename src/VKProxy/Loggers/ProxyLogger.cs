using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Threading.RateLimiting;
using VKProxy.Config;
using VKProxy.Core.Http;
using VKProxy.Features;
using VKProxy.Middlewares.Http;

namespace VKProxy.Core.Loggers;

public partial class ProxyLogger : ILogger
{
    internal readonly ILogger generalLogger;
    private readonly Meter? metrics;
    private readonly Counter<long> clusterFailedCounter;
    private readonly Counter<long> clusterActiveCounter;
    private readonly Counter<long>? requestsCounter;
    private readonly Histogram<double>? requestDuration;

    public ProxyLogger(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        generalLogger = loggerFactory.CreateLogger("VKProxy.Server.ReverseProxy");
        var f = serviceProvider.GetService<IMeterFactory>();
        metrics = f == null ? null : f.Create("VKProxy.ReverseProxy");
        if (metrics != null)
        {
            clusterFailedCounter = metrics.CreateCounter<long>("vkproxy.cluster.failed", description: "Number of proxy requests that have failed");
            clusterActiveCounter = metrics.CreateCounter<long>("vkproxy.cluster.active", description: "Number of requests that are currently active through the proxy");
            requestsCounter = metrics.CreateCounter<long>("vkproxy.requests", unit: "{request}", "Total number of (HTTP/tcp/udp) requests processed by the reverse proxy.");
            requestDuration = metrics.CreateHistogram(
            "vkproxy.request.duration",
            unit: "s",
            description: "Proxy handle duration of (HTTP/tcp/udp) requests.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300] });
            metrics.CreateObservableUpDownCounter<long>("vkproxy.rate_limit.current_available_permits", GetRateLimitMeasurements(serviceProvider, static s => s.CurrentAvailablePermits)
                , unit: "{request}", "Number of permits currently available for the ratelimiting");
            metrics.CreateObservableUpDownCounter<long>("vkproxy.rate_limit.current_queued_count", GetRateLimitMeasurements(serviceProvider, static s => s.CurrentQueuedCount)
                , unit: "{request}", "Number of queued permits for the ratelimiting");
            metrics.CreateObservableUpDownCounter<long>("vkproxy.rate_limit.total_failed_leases", GetRateLimitMeasurements(serviceProvider, static s => s.TotalFailedLeases)
                , unit: "{request}", "Total number of failed for the ratelimiting");
            metrics.CreateObservableUpDownCounter<long>("vkproxy.rate_limit.total_successful_leases", GetRateLimitMeasurements(serviceProvider, static s => s.TotalSuccessfulLeases)
                , unit: "{request}", "Total number of successful for the ratelimiting");
        }
    }

    private static Func<IEnumerable<Measurement<long>>> GetRateLimitMeasurements(IServiceProvider serviceProvider, Func<RateLimiterStatistics, long> func)
    {
        return () => serviceProvider.GetRequiredService<IConfigSource<IProxyConfig>>().CurrentSnapshot.Routes.Values.Where(static i => i.ConnectionLimiter != null)
                            .SelectMany(i =>
                            {
                                return i.ConnectionLimiter.GetAllLimiter().Select(j =>
                                {
                                    Measurement<long>? r;
                                    var s = j.Value.GetStatistics();
                                    if (s == null)
                                    {
                                        r = null;
                                    }
                                    else
                                    {
                                        var tags = new TagList
                                        {
                                    { "route", i.Key },
                                    { "key", j.Key}
                                        };
                                        return new Measurement<long>(func(s), in tags);
                                    }

                                    return r;
                                }).Where(static i => i.HasValue).Select(static i => i.Value);
                            });
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => generalLogger.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => generalLogger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => generalLogger.BeginScope(state);

    public void BindListenOptionsError(ListenEndPointOptions endPoint, Exception ex)
    {
        GeneralLog.BindListenOptionsError(generalLogger, endPoint, ex);
    }

    public void ErrorConfig(string message)
    {
        GeneralLog.ErrorConfig(generalLogger, message);
    }

    public void BindListenOptions(ListenEndPointOptions s)
    {
        GeneralLog.BindListenOptions(generalLogger, s);
    }

    public void UnexpectedException(string msg, Exception ex)
    {
        GeneralLog.UnexpectedException(generalLogger, msg, ex);
    }

    public void NotFoundActiveHealthCheckPolicy(string policy)
    {
        GeneralLog.NotFoundActiveHealthCheckPolicy(generalLogger, policy);
    }

    public void SocketConnectionCheckFailed(EndPoint endPoint, Exception ex)
    {
        GeneralLog.SocketConnectionCheckFailed(generalLogger, endPoint, ex.Message);
    }

    public void NotFoundAvailableUpstream(string clusterId)
    {
        GeneralLog.NotFoundAvailableUpstream(generalLogger, clusterId);
    }

    public void ConnectUpstreamTimeout(string routeId)
    {
        GeneralLog.ConnectUpstreamTimeout(generalLogger, routeId);
    }

    public void ProxyTimeout(string routeId, TimeSpan time)
    {
        GeneralLog.ProxyTimeout(generalLogger, routeId, time);
    }

    public void ProxyBegin(IReverseProxyFeature feature)
    {
        string routeId = GetRouteId(feature);
        GeneralLog.ProxyBegin(generalLogger, routeId);
        if (requestsCounter != null && requestsCounter.Enabled)
        {
            var tags = new TagList
            {
                { "route", routeId }
            };
            requestsCounter.Add(1, in tags);
        }
    }

    private static string GetRouteId(IReverseProxyFeature feature)
    {
        return feature.Route == null ? "(null)" : feature.Route.Key;
    }

    public void ProxyEnd(IReverseProxyFeature feature)
    {
        string routeId = GetRouteId(feature);
        GeneralLog.ProxyEnd(generalLogger, routeId);
        if (requestDuration != null && requestDuration.Enabled)
        {
            var endTimestamp = Stopwatch.GetTimestamp();
            var tags = new TagList
            {
                { "route", routeId }
            };
            requestDuration.Record(Stopwatch.GetElapsedTime(feature.StartTimestamp, endTimestamp).TotalSeconds, in tags);
        }
    }

    public void NotFoundRouteSni(string host)
    {
        GeneralLog.NotFoundRouteSni(generalLogger, host);
    }

    public void NotFoundRouteHttp(string host, string path)
    {
        GeneralLog.NotFoundRouteHttp(generalLogger, host, path);
    }

    public void NotProxying(int statusCode)
    {
        GeneralLog.NotProxying(generalLogger, statusCode);
    }

    public void Proxying(HttpRequestMessage msg, bool isStreamingRequest, IReverseProxyFeature proxyFeature, ClusterConfig cluster)
    {
        // Avoid computing the AbsoluteUri unless logging is enabled
        if (generalLogger.IsEnabled(LogLevel.Information))
        {
            var streaming = isStreamingRequest ? "streaming" : string.Empty;
            var version = HttpProtocol.GetHttpProtocol(msg.Version);
            var versionPolicy = ProtocolHelper.GetVersionPolicy(msg.VersionPolicy);
            GeneralLog.Proxying(generalLogger, msg.RequestUri!.AbsoluteUri, version, versionPolicy, streaming);
        }
        if (clusterActiveCounter != null && clusterActiveCounter.Enabled)
        {
            var tags = new TagList
            {
                { "route", proxyFeature.Route.Key },
                { "cluster", cluster.Key }
            };
            clusterActiveCounter.Add(1, in tags);
        }
    }

    public void RetryingWebSocketDowngradeNoConnect()
    {
        GeneralLog.RetryingWebSocketDowngradeNoConnect(generalLogger);
    }

    public void RetryingWebSocketDowngradeNoHttp2()
    {
        GeneralLog.RetryingWebSocketDowngradeNoHttp2(generalLogger);
    }

    public void ResponseReceived(HttpResponseMessage msg)
    {
        GeneralLog.ResponseReceived(generalLogger, msg.Version, msg.StatusCode);
    }

    public void ErrorProxying(ForwarderError error, Exception ex, HttpContext context)
    {
        var message = GetMessage(error);

        if (error is
            ForwarderError.RequestCanceled or
            ForwarderError.RequestBodyCanceled or
            ForwarderError.ResponseBodyCanceled or
            ForwarderError.UpgradeRequestCanceled or
            ForwarderError.UpgradeResponseCanceled)
        {
            // These error conditions are triggered by the client and are not generally indicative of a problem with the proxy.
            // It's unlikely that they will be useful in most cases, so we log them at Debug level to reduce noise.
            GeneralLog.ProxyRequestCancelled(generalLogger, error, message, ex);
        }
        else
        {
            GeneralLog.ProxyError(generalLogger, error, message, ex);
        }
        if (clusterFailedCounter != null && clusterFailedCounter.Enabled)
        {
            var f = context.Features.Get<IReverseProxyFeature>();
            var tags = new TagList
            {
                { "route", f.Route.Key },
                { "cluster", f.Route.Key }
            };
            clusterFailedCounter.Add(1, in tags);
        }
    }

    public void InvalidSecWebSocketKeyHeader(string? key)
    {
        GeneralLog.InvalidSecWebSocketKeyHeader(generalLogger, key);
    }

    public static string GetMessage(ForwarderError error)
    {
        return error switch
        {
            ForwarderError.None => throw new NotSupportedException("A more specific error must be used"),
            ForwarderError.Request => "An error was encountered before receiving a response.",
            ForwarderError.RequestCreation => "An error was encountered while creating the request message.",
            ForwarderError.RequestTimedOut => "The request timed out before receiving a response.",
            ForwarderError.RequestCanceled => "The request was canceled before receiving a response.",
            ForwarderError.RequestBodyCanceled => "Copying the request body was canceled.",
            ForwarderError.RequestBodyClient => "The client reported an error when copying the request body.",
            ForwarderError.RequestBodyDestination => "The destination reported an error when copying the request body.",
            ForwarderError.ResponseBodyCanceled => "Copying the response body was canceled.",
            ForwarderError.ResponseBodyClient => "The client reported an error when copying the response body.",
            ForwarderError.ResponseBodyDestination => "The destination reported an error when copying the response body.",
            ForwarderError.ResponseHeaders => "The destination returned a response that cannot be proxied back to the client.",
            ForwarderError.UpgradeRequestCanceled => "Copying the upgraded request body was canceled.",
            ForwarderError.UpgradeRequestClient => "The client reported an error when copying the upgraded request body.",
            ForwarderError.UpgradeRequestDestination => "The destination reported an error when copying the upgraded request body.",
            ForwarderError.UpgradeResponseCanceled => "Copying the upgraded response body was canceled.",
            ForwarderError.UpgradeResponseClient => "The client reported an error when copying the upgraded response body.",
            ForwarderError.UpgradeResponseDestination => "The destination reported an error when copying the upgraded response body.",
            ForwarderError.UpgradeActivityTimeout => "The WebSocket connection was closed after being idle longer than the Activity Timeout.",
            ForwarderError.NoAvailableDestinations => throw new NotImplementedException(), // Not used in this class
            _ => throw new NotImplementedException(error.ToString()),
        };
    }

    public void ConnectionRejected(string connectionId)
    {
        GeneralLog.ConnectionRejected(generalLogger, connectionId);
    }

    internal void ReportFailed(RouteConfig route)
    {
        if (clusterFailedCounter != null && clusterFailedCounter.Enabled)
        {
            var tags = new TagList
            {
                { "route", route.Key },
                { "cluster", route.ClusterConfig.Key }
            };
            clusterFailedCounter.Add(1, in tags);
        }
    }

    internal void ProxyingEnd(IReverseProxyFeature proxyFeature, ClusterConfig cluster)
    {
        if (clusterActiveCounter != null && clusterActiveCounter.Enabled)
        {
            var tags = new TagList
            {
                { "route", proxyFeature.Route.Key },
                { "cluster", cluster.Key }
            };
            clusterActiveCounter.Add(-1, in tags);
        }
    }

    internal void Proxying(RouteConfig route)
    {
        if (clusterActiveCounter != null && clusterActiveCounter.Enabled)
        {
            var tags = new TagList
            {
                { "route", route.Key },
                { "cluster", route.ClusterConfig.Key }
            };
            clusterActiveCounter.Add(1, in tags);
        }
    }
}

internal static partial class GeneralLog
{
    [LoggerMessage(0, LogLevel.Error, @"Unexpected exception {Msg}.", EventName = "UnexpectedException", SkipEnabledCheck = true)]
    public static partial void UnexpectedException(ILogger logger, string msg, Exception ex);

    [LoggerMessage(1, LogLevel.Critical, @"Unable to bind to {Endpoint} on config reload.", EventName = "BindListenOptionsError")]
    public static partial void BindListenOptionsError(ILogger logger, ListenEndPointOptions endpoint, Exception ex);

    [LoggerMessage(2, LogLevel.Warning, @"{msg}", EventName = "ErrorConfig")]
    public static partial void ErrorConfig(this ILogger logger, string msg);

    [LoggerMessage(3, LogLevel.Information, @"Listening on: {s}", EventName = "BindListenOptions")]
    public static partial void BindListenOptions(ILogger logger, ListenEndPointOptions s);

    [LoggerMessage(4, LogLevel.Warning, @"Not found active health check policy {policy}.", EventName = "NotFoundActiveHealthCheckPolicy")]
    public static partial void NotFoundActiveHealthCheckPolicy(ILogger logger, string policy);

    [LoggerMessage(5, LogLevel.Warning, @"Active health failed, can not connect socket {endPoint} {ex}.", EventName = "SocketConnectionCheckFailed")]
    public static partial void SocketConnectionCheckFailed(ILogger logger, EndPoint endPoint, string ex);

    [LoggerMessage(6, LogLevel.Warning, @"Not found available upstream for cluster ""{ClusterId}"".", EventName = "NotFoundAvailableUpstream")]
    public static partial void NotFoundAvailableUpstream(ILogger logger, string clusterId);

    [LoggerMessage(7, LogLevel.Information, @"Connect upstream timeout for route {routeId}.", EventName = "ConnectUpstreamTimeout")]
    public static partial void ConnectUpstreamTimeout(ILogger logger, string routeId);

    [LoggerMessage(8, LogLevel.Information, @"Proxy timeout ({time}) for route {routeId}.", EventName = "ProxyTimeout")]
    public static partial void ProxyTimeout(ILogger logger, string routeId, TimeSpan time);

    [LoggerMessage(9, LogLevel.Information, @"Begin proxy for route {routeId}.", EventName = "ProxyBegin")]
    public static partial void ProxyBegin(ILogger logger, string routeId);

    [LoggerMessage(10, LogLevel.Information, @"End proxy for route {routeId}.", EventName = "ProxyEnd")]
    public static partial void ProxyEnd(ILogger logger, string routeId);

    [LoggerMessage(11, LogLevel.Information, @"Not found sni route for ""{host}"".", EventName = "NotFoundRouteSni")]
    public static partial void NotFoundRouteSni(ILogger logger, string host);

    [LoggerMessage(12, LogLevel.Information, @"Not found http route for ""{host} {path}"".", EventName = "NotFoundRouteHttp")]
    public static partial void NotFoundRouteHttp(ILogger logger, string host, string path);

    [LoggerMessage(13, LogLevel.Information, "Not Proxying, a {statusCode} response was set by the transforms.", EventName = "NotForwarding")]
    public static partial void NotProxying(ILogger logger, int statusCode);

    [LoggerMessage(14, LogLevel.Information, "Proxying to {targetUrl} {version} {versionPolicy} {isStreaming}", EventName = "Forwarding", SkipEnabledCheck = true)]
    public static partial void Proxying(ILogger logger, string targetUrl, string version, string versionPolicy, string isStreaming);

    [LoggerMessage(15, LogLevel.Information, "Received HTTP/{version} response {statusCode}.", EventName = "ResponseReceived")]
    public static partial void ResponseReceived(ILogger logger, Version version, HttpStatusCode statusCode);

    [LoggerMessage(16, LogLevel.Information, "Unable to proxy the WebSocket using HTTP/2, the server does not support RFC 8441, retrying with HTTP/1.1.", EventName = "RetryingWebSocketDowngradeNoConnect")]
    public static partial void RetryingWebSocketDowngradeNoConnect(ILogger logger);

    [LoggerMessage(17, LogLevel.Information, "Unable to proxy the WebSocket using HTTP/2, server does not support HTTP/2. Retrying with HTTP/1.1. Disable HTTP/2 negotiation for improved performance.", EventName = "RetryingWebSocketDowngradeNoHttp2")]
    public static partial void RetryingWebSocketDowngradeNoHttp2(ILogger logger);

    [LoggerMessage(18, LogLevel.Warning, "{error}: {message}", EventName = "ForwardingError")]
    public static partial void ProxyError(ILogger logger, ForwarderError error, string message, Exception ex);

    [LoggerMessage(19, LogLevel.Debug, "{error}: {message}", EventName = "ForwardingRequestCancelled")]
    public static partial void ProxyRequestCancelled(ILogger logger, ForwarderError error, string message, Exception ex);

    [LoggerMessage(20, LogLevel.Warning, "Invalid Sec-WebSocket-Key header: '{key}'.", EventName = "InvalidSecWebSocketKeyHeader")]
    public static partial void InvalidSecWebSocketKeyHeader(ILogger logger, string key);

    [LoggerMessage(21, LogLevel.Warning, @"Connection id ""{ConnectionId}"" rejected because the maximum number of concurrent connections has been reached.", EventName = "ConnectionRejected")]
    public static partial void ConnectionRejected(ILogger logger, string connectionId);

    [LoggerMessage(22, LogLevel.Debug, "The response time of the entry is {ResponseTime} and has exceeded the expiry date of {Expired} specified by the 'Expires' header.",
        EventName = "ExpirationExpiresExceeded")]
    internal static partial void ExpirationExpiresExceeded(this ILogger logger, DateTimeOffset responseTime, DateTimeOffset expired);

    [LoggerMessage(23, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive.", EventName = "ExpirationMaxAgeExceeded")]
    internal static partial void ExpirationMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan maxAge);

    [LoggerMessage(24, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age for shared caches of {SharedMaxAge} specified by the 's-maxage' cache directive.",
        EventName = "ExpirationSharedMaxAgeExceeded")]
    internal static partial void ExpirationSharedMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan sharedMaxAge);

    [LoggerMessage(25, LogLevel.Debug, "Response is not cacheable because its status code {StatusCode} does not indicate success.",
        EventName = "ResponseWithUnsuccessfulStatusCodeNotCacheable")]
    internal static partial void ResponseWithUnsuccessfulStatusCodeNotCacheable(this ILogger logger, int statusCode);

    [LoggerMessage(26, LogLevel.Debug, "Response is not cacheable because it contains a 'no-cache' cache directive.",
        EventName = "ResponseWithNoCacheNotCacheable")]
    internal static partial void ResponseWithNoCacheNotCacheable(this ILogger logger);

    [LoggerMessage(27, LogLevel.Debug, "Response is not cacheable because it contains a 'SetCookie' header.", EventName = "ResponseWithSetCookieNotCacheable")]
    internal static partial void ResponseWithSetCookieNotCacheable(this ILogger logger);

    [LoggerMessage(28, LogLevel.Debug, "Response is not cacheable because it contains a '.Vary' header with a value of *.",
        EventName = "ResponseWithVaryStarNotCacheable")]
    internal static partial void ResponseWithVaryStarNotCacheable(this ILogger logger);

    [LoggerMessage(29, LogLevel.Debug, "Response is not cacheable because it contains the 'private' cache directive.",
        EventName = "ResponseWithPrivateNotCacheable")]
    internal static partial void ResponseWithPrivateNotCacheable(this ILogger logger);

    [LoggerMessage(30, LogLevel.Debug, "Response is not cacheable because it does not contain the 'public' cache directive.",
        EventName = "ResponseWithoutPublicNotCacheable")]
    internal static partial void ResponseWithoutPublicNotCacheable(this ILogger logger);

    [LoggerMessage(31, LogLevel.Debug, "Response is not cacheable because it or its corresponding request contains a 'no-store' cache directive.",
        EventName = "ResponseWithNoStoreNotCacheable")]
    internal static partial void ResponseWithNoStoreNotCacheable(this ILogger logger);

    [LoggerMessage(32, LogLevel.Information, "The response has been cached.", EventName = "ResponseCached")]
    internal static partial void ResponseCached(this ILogger logger);

    [LoggerMessage(33, LogLevel.Information, "The response could not be cached for this request.", EventName = "ResponseNotCached")]
    internal static partial void LogResponseNotCached(this ILogger logger);

    [LoggerMessage(34, LogLevel.Warning, "The response could not be cached for this request because the 'Content-Length' did not match the body length.",
        EventName = "responseContentLengthMismatchNotCached")]
    internal static partial void ResponseContentLengthMismatchNotCached(this ILogger logger);

    [LoggerMessage(35, LogLevel.Information, "No cached response available for this request and the 'only-if-cached' cache directive was specified.",
        EventName = "GatewayTimeoutServed")]
    internal static partial void GatewayTimeoutServed(this ILogger logger);

    [LoggerMessage(36, LogLevel.Information, "No cached response available for this request.", EventName = "NoResponseServed")]
    internal static partial void NoResponseServed(this ILogger logger);

    [LoggerMessage(37, LogLevel.Information, "The content requested has not been modified.", EventName = "NotModifiedServed")]
    internal static partial void NotModifiedServed(this ILogger logger);

    [LoggerMessage(38, LogLevel.Information, "Serving response from cache.", EventName = "CachedResponseServed")]
    internal static partial void CachedResponseServed(this ILogger logger);

    [LoggerMessage(39, LogLevel.Debug, "The 'IfNoneMatch' header of the request contains a value of *.", EventName = "NotModifiedIfNoneMatchStar")]
    internal static partial void NotModifiedIfNoneMatchStar(this ILogger logger);

    [LoggerMessage(40, LogLevel.Debug, "The ETag {ETag} in the 'IfNoneMatch' header matched the ETag of a cached entry.",
        EventName = "NotModifiedIfNoneMatchMatched")]
    internal static partial void NotModifiedIfNoneMatchMatched(this ILogger logger, EntityTagHeaderValue etag);

    [LoggerMessage(41, LogLevel.Debug, "The last modified date of {LastModified} is before the date {IfModifiedSince} specified in the 'IfModifiedSince' header.",
        EventName = "NotModifiedIfModifiedSinceSatisfied")]
    internal static partial void NotModifiedIfModifiedSinceSatisfied(this ILogger logger, DateTimeOffset lastModified, DateTimeOffset ifModifiedSince);

    [LoggerMessage(42, LogLevel.Debug, "Adding a minimum freshness requirement of {Duration} specified by the 'min-fresh' cache directive.",
        EventName = "LogRequestMethodNotCacheable")]
    internal static partial void ExpirationMinFreshAdded(this ILogger logger, TimeSpan duration);

    [LoggerMessage(43, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. " +
        "It must be revalidated because the 'must-revalidate' or 'proxy-revalidate' cache directive is specified.",
        EventName = "ExpirationMustRevalidate")]
    internal static partial void ExpirationMustRevalidate(this ILogger logger, TimeSpan age, TimeSpan maxAge);

    [LoggerMessage(44, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. " +
    "However, it satisfied the maximum stale allowance of {MaxStale} specified by the 'max-stale' cache directive.",
    EventName = "ExpirationMaxStaleSatisfied")]
    internal static partial void ExpirationMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge, TimeSpan maxStale);

    [LoggerMessage(45, LogLevel.Debug,
        "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. " +
        "However, the 'max-stale' cache directive was specified without an assigned value and a stale response of any age is accepted.",
        EventName = "ExpirationInfiniteMaxStaleSatisfied")]
    internal static partial void ExpirationInfiniteMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge);
}