namespace Sentry.Serilog;

/// <summary>
/// Sentry Sink for Serilog
/// </summary>
/// <inheritdoc cref="IDisposable" />
/// <inheritdoc cref="ILogEventSink" />
internal sealed class SentrySink : ILogEventSink, IDisposable
{
    private readonly IDisposable? _sdkDisposable;
    private readonly SentrySerilogOptions _options;

    internal static readonly SdkVersion NameAndVersion
        = typeof(SentrySink).Assembly.GetNameAndVersion();

    /// <summary>
    /// Serilog SDK name.
    /// </summary>
    public const string SdkName = "sentry.dotnet.serilog";

    private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

    private readonly Func<IHub> _hubAccessor;
    private readonly ISystemClock _clock;

    public SentrySink(
        SentrySerilogOptions options,
        IDisposable? sdkDisposable)
        : this(
            options,
            () => HubAdapter.Instance,
            sdkDisposable,
            SystemClock.Clock)
    {
    }

    internal SentrySink(
        SentrySerilogOptions options,
        Func<IHub> hubAccessor,
        IDisposable? sdkDisposable,
        ISystemClock clock)
    {
        _options = options;
        _hubAccessor = hubAccessor;
        _clock = clock;
        _sdkDisposable = sdkDisposable;
    }

    private static AsyncLocal<bool> isReentrant = new();

    public void Emit(LogEvent logEvent)
    {
        if (isReentrant.Value)
        {
            _options.DiagnosticLogger?.LogError($"Reentrant log event detected. Logging when inside the scope of another log event can cause a StackOverflowException. LogEventInfo.Message: {logEvent.MessageTemplate.Text}");
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
            if (IsSentryContext(context))
            {
                return;
            }
        }

        var hub = _hubAccessor();
        if (hub is null || !hub.IsEnabled)
        {
            return;
        }

        var exception = logEvent.Exception;
        var template = logEvent.MessageTemplate.Text;
        var formatted = FormatLogEvent(logEvent);

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

            if (evt.Sdk is { } sdk)
            {
                sdk.Name = SdkName;
                sdk.Version = NameAndVersion.Version;

                if (NameAndVersion.Version is { } version)
                {
                    sdk.AddPackage(ProtocolPackageName, version);
                }
            }

            evt.SetExtras(GetLoggingEventProperties(logEvent));

            hub.CaptureEvent(evt);

            // Capturing exception events adds a breadcrumb automatically... we don't want our sink to add another one
            return;
        }

        if (logEvent.Level < _options.MinimumBreadcrumbLevel)
        {
            return;
        }

        Dictionary<string, string>? data = null;
        if (exception != null && !string.IsNullOrWhiteSpace(formatted))
        {
            // Exception.Message won't be used as Breadcrumb message
            // Avoid losing it by adding as data:
            data = new Dictionary<string, string>
            {
                {"exception_message", exception.Message}
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

    private static bool IsSentryContext(string context) =>
        context.StartsWith("Sentry.") ||
        string.Equals(context, "Sentry", StringComparison.Ordinal);

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

    public void Dispose() => _sdkDisposable?.Dispose();
}
