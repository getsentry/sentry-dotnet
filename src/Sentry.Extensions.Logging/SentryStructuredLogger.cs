using Microsoft.Extensions.Logging;
using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Extensions.Logging;

internal sealed class SentryStructuredLogger : ILogger
{
    private readonly string? _categoryName;
    private readonly SentryLoggingOptions _options;
    private readonly IHub _hub;
    private readonly ISystemClock _clock;
    private readonly SdkVersion _sdk;

    internal SentryStructuredLogger(string categoryName, SentryLoggingOptions options, IHub hub, ISystemClock clock, SdkVersion sdk)
    {
        _categoryName = categoryName;
        _options = options;
        _clock = clock;
        _hub = hub;
        _sdk = sdk;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return NullDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _hub.IsEnabled
            && _options.EnableLogs
            && logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var timestamp = _clock.GetUtcNow();
        SentryLog.GetTraceIdAndSpanId(_hub, out var traceId, out var spanId);

        var level = logLevel.ToSentryLogLevel();
        Debug.Assert(level != default);

        string message;
        try
        {
            message = formatter.Invoke(state, exception);
        }
        catch (FormatException e)
        {
            _options.DiagnosticLogger?.LogError(e, "Template string does not match the provided argument. The Log will be dropped.");
            return;
        }

        string? template = null;
        var parameters = ImmutableArray.CreateBuilder<KeyValuePair<string, object>>();
        // see Microsoft.Extensions.Logging.FormattedLogValues
        if (state is IReadOnlyList<KeyValuePair<string, object?>> formattedLogValues)
        {
            if (formattedLogValues.Count != 0)
            {
                parameters.Capacity = formattedLogValues.Count - 1;
            }

            foreach (var formattedLogValue in formattedLogValues)
            {
                if (formattedLogValue.Key == "{OriginalFormat}" && formattedLogValue.Value is string formattedString)
                {
                    template = formattedString;
                }
                else if (formattedLogValue.Value is not null)
                {
                    parameters.Add(new KeyValuePair<string, object>(formattedLogValue.Key, formattedLogValue.Value));
                }
            }
        }

        SentryLog log = new(timestamp, traceId, level, message)
        {
            Template = template,
            Parameters = parameters.DrainToImmutable(),
            SpanId = spanId,
        };

        log.SetDefaultAttributes(_options, _hub.GetScope(), _sdk);
        log.SetOrigin("auto.log.extensions_logging");

        if (_categoryName is not null)
        {
            log.SetAttribute("category.name", _categoryName);
        }
        if (eventId.Name is not null || eventId.Id != 0)
        {
            log.SetAttribute("event.id", eventId.Id);
        }
        if (eventId.Name is not null)
        {
            log.SetAttribute("event.name", eventId.Name);
        }

        _hub.Logger.CaptureLog(log);
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
