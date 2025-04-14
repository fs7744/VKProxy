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

    public void BindListenOptionsError(ListenConfig endPoint, Exception ex)
    {
        GeneralLog.BindListenOptionsError(generalLogger, endPoint, ex);
    }

    private static partial class GeneralLog
    {
        [LoggerMessage(2, LogLevel.Critical, @"Unable to bind to {Endpoint} on config reload.", EventName = "BindListenOptionsError")]
        public static partial void BindListenOptionsError(ILogger logger, ListenConfig endpoint, Exception ex);
    }
}