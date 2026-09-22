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

    private int _registeredScopeEventProcessor;

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

        // Sentry is already initialised when SentrySdk.Init runs before the Serilog configuration.
        // Otherwise InnerEmit registers on the first log event instead.
        if (hubAccessor() is { IsEnabled: true } hub && hub.GetSentryOptions() is { } sentryOptions)
        {
            EnsureSerilogScopeEventProcessor(sentryOptions);
        }
    }

    private static AsyncLocal<bool> isReentrant = new();

    public void Emit(LogEvent logEvent)
    {
        // Must precede the reentrancy check below: the SDK's own diagnostics are routed back through
        // Serilog, and answering them with another diagnostic is what makes the feedback loop endless.
        logEvent.TryGetSourceContext(out var context);
        if (SentrySdkNamespaces.IsSentrySdk(context))
        {
            return;
        }

        if (isReentrant.Value)
        {
            _hubAccessor()?.GetSentryOptions()?.DiagnosticLogger?.LogError($"Reentrant log event detected. Logging when inside the scope of another log event can cause a StackOverflowException. LogEventInfo.Message: {logEvent.MessageTemplate.Text}");
            return;
        }

        isReentrant.Value = true;
        try
        {
            InnerEmit(logEvent, context);
        }
        finally
        {
            isReentrant.Value = false;
        }
    }

    private void InnerEmit(LogEvent logEvent, string? context)
    {
        if (_hubAccessor() is not { IsEnabled: true } hub)
        {
            return;
        }

        var options = hub.GetSentryOptions();
        if (options is not null)
        {
            EnsureSerilogScopeEventProcessor(options);
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

    private void EnsureSerilogScopeEventProcessor(SentryOptions options)
    {
        if (Interlocked.Exchange(ref _registeredScopeEventProcessor, 1) != 0)
        {
            return;
        }

        if (options.HasSerilogScopeEventProcessor())
        {
            return;
        }

        options.UseSerilog();
        options.LogWarning(
            "The Sentry sink for Serilog registered the Serilog scope event processor automatically, because " +
            "UseSerilog() was not called on the options used to initialise Sentry. Events captured before the sink " +
            "received its first log event will not have properties from the Serilog LogContext applied. Call " +
            "UseSerilog() when initialising Sentry to apply them to every event.");
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
