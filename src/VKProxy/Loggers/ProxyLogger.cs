using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using VKProxy.Config;
using VKProxy.Core.Http;
using VKProxy.Middlewares.Http;

namespace VKProxy.Core.Loggers;

public partial class ProxyLogger : ILogger
{
    private readonly ILogger generalLogger;

    public ProxyLogger(ILoggerFactory loggerFactory)
    {
        generalLogger = loggerFactory.CreateLogger("VKProxy.Server.ReverseProxy");
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

    public void ProxyBegin(string routeId)
    {
        GeneralLog.ProxyBegin(generalLogger, routeId);
    }

    public void ProxyEnd(string routeId)
    {
        GeneralLog.ProxyEnd(generalLogger, routeId);
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

    public void Proxying(HttpRequestMessage msg, bool isStreamingRequest)
    {
        // Avoid computing the AbsoluteUri unless logging is enabled
        if (generalLogger.IsEnabled(LogLevel.Information))
        {
            var streaming = isStreamingRequest ? "streaming" : string.Empty;
            var version = HttpProtocol.GetHttpProtocol(msg.Version);
            var versionPolicy = ProtocolHelper.GetVersionPolicy(msg.VersionPolicy);
            GeneralLog.Proxying(generalLogger, msg.RequestUri!.AbsoluteUri, version, versionPolicy, streaming);
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

    public void ErrorProxying(ForwarderError error, Exception ex)
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

    private static partial class GeneralLog
    {
        [LoggerMessage(0, LogLevel.Error, @"Unexpected exception {Msg}.", EventName = "UnexpectedException", SkipEnabledCheck = true)]
        public static partial void UnexpectedException(ILogger logger, string msg, Exception ex);

        [LoggerMessage(1, LogLevel.Critical, @"Unable to bind to {Endpoint} on config reload.", EventName = "BindListenOptionsError")]
        public static partial void BindListenOptionsError(ILogger logger, ListenEndPointOptions endpoint, Exception ex);

        [LoggerMessage(2, LogLevel.Warning, @"{msg}", EventName = "ErrorConfig")]
        public static partial void ErrorConfig(ILogger logger, string msg);

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
    }
}