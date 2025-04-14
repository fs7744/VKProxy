using Microsoft.Extensions.Logging;
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

    public void IngoreErrorConfig(string message)
    {
        GeneralLog.IngoreErrorConfig(generalLogger, message);
    }

    public void BindListenOptions(ListenEndPointOptions s)
    {
        GeneralLog.BindListenOptions(generalLogger, s);
    }

    private static partial class GeneralLog
    {
        [LoggerMessage(0, LogLevel.Critical, @"Unable to bind to {Endpoint} on config reload.", EventName = "BindListenOptionsError")]
        public static partial void BindListenOptionsError(ILogger logger, ListenEndPointOptions endpoint, Exception ex);

        [LoggerMessage(1, LogLevel.Warning, @"Ingore error config {msg}", EventName = "IngoreErrorConfig")]
        public static partial void IngoreErrorConfig(ILogger logger, string msg);

        [LoggerMessage(2, LogLevel.Information, @"Listening on: {s}", EventName = "BindListenOptions")]
        public static partial void BindListenOptions(ILogger logger, ListenEndPointOptions s);
    }
}