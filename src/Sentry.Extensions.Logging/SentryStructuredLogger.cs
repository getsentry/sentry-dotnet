using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

[Experimental(Infrastructure.DiagnosticId.ExperimentalFeature)]
internal sealed class SentryStructuredLogger : ILogger
{
    private readonly string _categoryName;
    private readonly SentryLoggingOptions _options;
    private readonly IHub _hub;

    internal SentryStructuredLogger(string categoryName, SentryLoggingOptions options, IHub hub)
    {
        _categoryName = categoryName;
        _options = options;
        _hub = hub;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return NullDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _hub.IsEnabled
            && _options.Experimental.EnableLogs
            && logLevel != LogLevel.None
            && logLevel >= _options.ExperimentalLogging.MinimumLogLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // not quite ideal as this is a boxing allocation from Microsoft.Extensions.Logging.FormattedLogValues
        /*
        string? template = null;
        object[]? parameters = null;
        if (state is IReadOnlyList<KeyValuePair<string, object?>> formattedLogValues)
        {
            foreach (var formattedLogValue in formattedLogValues)
            {
                if (formattedLogValue.Key == "{OriginalFormat}" && formattedLogValue.Value is string formattedString)
                {
                    template = formattedString;
                    break;
                }
            }
        }
        */

        string message = formatter.Invoke(state, exception);

        switch (logLevel)
        {
            case LogLevel.Trace:
                _hub.Logger.LogTrace(message);
                break;
            case LogLevel.Debug:
                _hub.Logger.LogDebug(message);
                break;
            case LogLevel.Information:
                _hub.Logger.LogInfo(message);
                break;
            case LogLevel.Warning:
                _hub.Logger.LogWarning(message);
                break;
            case LogLevel.Error:
                _hub.Logger.LogError(message);
                break;
            case LogLevel.Critical:
                _hub.Logger.LogFatal(message);
                break;
            case LogLevel.None:
            default:
                break;
        }
    }
}

file sealed class NullDisposable : IDisposable
{
    public static NullDisposable Instance { get; } = new NullDisposable();

    private NullDisposable()
    {
    }

    public void Dispose()
    {
    }
}
