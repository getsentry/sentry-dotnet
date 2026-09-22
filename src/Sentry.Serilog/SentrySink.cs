namespace Sentry.Serilog;

/// <summary>
/// Sentry Sink for Serilog
/// </summary>
/// <inheritdoc cref="ILogEventSink" />
internal sealed partial class SentrySink : ILogEventSink
{
    private readonly SentrySerilogOptions _options;

    private readonly Func<IHub> _hubAccessor;
    private readonly ISystemClock _clock;

    private int _checkedUseSerilog;

    public SentrySink(SentrySerilogOptions options)
        : this(
            options,
            () => HubAdapter.Instance,
            SystemClock.Clock)
    {
    }

    internal SentrySink(
        SentrySerilogOptions options,
        Func<IHub> hubAccessor,
        ISystemClock clock)
    {
        _options = options;
        _hubAccessor = hubAccessor;
        _clock = clock;
    }

    private static AsyncLocal<bool> isReentrant = new();

    public void Emit(LogEvent logEvent)
    {
        if (isReentrant.Value)
        {
            _hubAccessor()?.GetSentryOptions()?.DiagnosticLogger?.LogError($"Reentrant log event detected. Logging when inside the scope of another log event can cause a StackOverflowException. LogEventInfo.Message: {logEvent.MessageTemplate.Text}");
            return;
        }

        isReentrant.Value = true;
        try
        {
            InnerEmit(logEvent);
        }
        finally
        {
            isReentrant.Value = false;
        }
    }

    private void InnerEmit(LogEvent logEvent)
    {
        if (logEvent.TryGetSourceContext(out var context))
        {
            if (SentrySdkNamespaces.IsSentrySdk(context))
            {
                return;
            }
        }

        if (_hubAccessor() is not { IsEnabled: true } hub)
        {
            return;
        }

        var options = hub.GetSentryOptions();
        if (options is not null)
        {
            WarnIfUseSerilogNotCalled(options);
        }

        var exception = logEvent.Exception;
        var template = logEvent.MessageTemplate.Text;
        var formatted = FormatLogEvent(logEvent);
        var addedBreadcrumbForException = false;

        if (logEvent.Level >= _options.MinimumEventLevel)
        {
            var evt = new SentryEvent(exception)
            {
                Logger = context,
                Message = new SentryMessage
                {
                    Formatted = formatted,
                    Message = template
                },
                Level = logEvent.Level.ToSentryLevel()
            };

            evt.SetExtras(GetLoggingEventProperties(logEvent));

            hub.CaptureEvent(evt);

            // Capturing exception events adds a breadcrumb automatically... we don't want to add another one
            if (exception != null)
            {
                addedBreadcrumbForException = true;
            }
        }

        if (!addedBreadcrumbForException && logEvent.Level >= _options.MinimumBreadcrumbLevel)
        {
            Dictionary<string, string>? data = null;
            if (exception != null && !string.IsNullOrWhiteSpace(formatted))
            {
                // Exception.Message won't be used as Breadcrumb message
                // Avoid losing it by adding as data:
                data = new Dictionary<string, string>
                {
                    { "exception_message", exception.Message }
                };
            }

            hub.AddBreadcrumb(
                _clock,
                string.IsNullOrWhiteSpace(formatted)
                    ? exception?.Message ?? ""
                    : formatted,
                context,
                data: data,
                level: logEvent.Level.ToBreadcrumbLevel());
        }

        if (options is not null)
        {
            CaptureStructuredLog(hub, options, logEvent, formatted, template);
        }
    }

    private void WarnIfUseSerilogNotCalled(SentryOptions options)
    {
        if (Interlocked.Exchange(ref _checkedUseSerilog, 1) != 0)
        {
            return;
        }

        if (!options.HasSerilogScopeEventProcessor())
        {
            options.LogWarning(
                "The Sentry sink for Serilog is in use, but UseSerilog() was not called on the options used to initialise Sentry. " +
                "Properties from the Serilog LogContext will not be applied to Sentry events.");
        }
    }

    private string FormatLogEvent(LogEvent logEvent)
    {
        if (_options.TextFormatter is { } formatter)
        {
            using var writer = new StringWriter();
            formatter.Format(logEvent, writer);
            return writer.ToString();
        }

        return logEvent.RenderMessage(_options.FormatProvider);
    }

    private static IEnumerable<KeyValuePair<string, object?>> GetLoggingEventProperties(LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties)
        {
            var value = property.Value;
            if (value is ScalarValue scalarValue)
            {
                yield return new KeyValuePair<string, object?>(property.Key, scalarValue.Value);
            }
            else if (value != null)
            {
                yield return new KeyValuePair<string, object?>(property.Key, value);
            }
        }
    }
}
