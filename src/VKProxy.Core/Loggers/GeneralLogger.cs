using Microsoft.Extensions.Logging;

namespace VKProxy.Core.Loggers;

public partial class GeneralLogger : ILogger
{
    private readonly ILogger generalLogger;

    public GeneralLogger(ILoggerFactory loggerFactory)
    {
        generalLogger = loggerFactory.CreateLogger("VKProxy.Server");
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => generalLogger.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => generalLogger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => generalLogger.BeginScope(state);

    public void UnexpectedException(string msg, Exception ex)
    {
        GeneralLog.UnexpectedException(generalLogger, msg, ex);
    }

    public void ConnectionReset(string connectionId)
    {
        GeneralLog.ConnectionReset(generalLogger, connectionId);
    }

    private static partial class GeneralLog
    {
        [LoggerMessage(0, LogLevel.Error, @"Unexpected exception {Msg}.", EventName = "UnexpectedException", SkipEnabledCheck = true)]
        public static partial void UnexpectedException(ILogger logger, string msg, Exception ex);

        [LoggerMessage(1, LogLevel.Debug, @"Connection id ""{ConnectionId}"" reset.", EventName = "ConnectionReset")]
        public static partial void ConnectionReset(ILogger logger, string connectionId);
    }
}