using Microsoft.Extensions.Logging;
using System.Net;
using VKProxy.Config;

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
    }
}