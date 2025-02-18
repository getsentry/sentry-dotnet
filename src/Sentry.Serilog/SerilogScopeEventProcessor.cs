using Serilog.Context;

namespace Sentry.Serilog;

/// <summary>
/// Sentry event processor that applies properties from the Serilog scope to Sentry events.
/// </summary>
internal class SerilogScopeEventProcessor : ISentryEventProcessor
{
    private readonly SentryOptions _options;

    /// <summary>
    /// This processor extracts properties from the Serilog context and applies these to Sentry events.
    /// </summary>
    public SerilogScopeEventProcessor(SentryOptions options)
    {
        _options = options;
        _options.LogDebug("Initializing Serilog scope event processor.");
    }

    /// <inheritdoc cref="ISentryEventProcessor"/>
    public SentryEvent Process(SentryEvent @event)
    {
        _options.LogDebug("Running Serilog scope event processor on: Event {0}", @event.EventId);

        // This is a bit of a hack. Serilog doesn't have any hooks that let us inspect the context. We can, however,
        // apply the context to a dummy log event and then copy across the properties from that log event to our Sentry
        // event.
        // See: https://github.com/getsentry/sentry-dotnet/issues/3544#issuecomment-2307884977
        var enricher = LogContext.Clone();
        var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Error, null, MessageTemplate.Empty, []);
        enricher.Enrich(logEvent, new LogEventPropertyFactory());
        foreach (var (key, value) in logEvent.Properties)
        {
            if (!@event.Tags.ContainsKey(key))
            {
                // Potentially we could be doing SetData here instead of SetTag. See DefaultSentryScopeStateProcessor.
                @event.SetTag(key, value.ToString());
            }
        }
        return @event;
    }

    private class LogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            var scalarValue = new ScalarValue(value);
            return new LogEventProperty(name, scalarValue);
        }
    }
}
