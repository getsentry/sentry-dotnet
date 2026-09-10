namespace Sentry.Log4Net;

public partial class SentryAppender
{
    private static void CaptureStructuredLog(IHub hub, SentryOptions options, LoggingEvent loggingEvent, string? environment, bool sendIdentity)
    {
        if (loggingEvent.ToSentryLogLevel() is not { } level)
        {
            return;
        }

        DateTimeOffset timestamp = new(loggingEvent.TimeStampUtc);
        const string? template = null; // cannot get format-string from `log4net.Util.SystemStringFormat` via `LoggingEvent.MessageObject`
        var parameters = ImmutableArray<KeyValuePair<string, object>>.Empty; // cannot get arguments from `log4net.Util.SystemStringFormat` via `LoggingEvent.MessageObject`

        var message = !string.IsNullOrWhiteSpace(loggingEvent.RenderedMessage) ? loggingEvent.RenderedMessage : string.Empty;
        var log = SentryLog.Create(hub, timestamp, level, message, template, parameters);

        var scope = hub.GetScope();
        log.SetDefaultAttributes(options, scope, Sdk);
        log.SetOrigin("auto.log.log4net");

        // Honor the appender-level settings, overriding the scope/options defaults, to match the SentryEvent path.
        if (!string.IsNullOrWhiteSpace(environment))
        {
            log.SetAttribute("sentry.environment", environment!);
        }

        if (sendIdentity && !string.IsNullOrEmpty(loggingEvent.Identity))
        {
            log.SetAttribute("user.id", loggingEvent.Identity);
        }

        if (loggingEvent.LoggerName is { } loggerName)
        {
            log.SetAttribute("category.name", loggerName);
        }

        // GetProperties() is non-null in current log4net, but the sibling GetLoggingEventProperties guards
        // against null, so we do too rather than rely on log4net's internals.
        if (loggingEvent.GetProperties() is { } properties)
        {
            // Read through GetKeys()/the indexer rather than enumerating the dictionary. log4net 3.x added
            // IDictionary<string, object?> to PropertiesDictionary, which changed what its non-generic
            // IEnumerable yields (KeyValuePair<string, object?> instead of DictionaryEntry), so enumerating
            // silently matched nothing there. GetKeys() behaves the same on both majors.
            foreach (var key in properties.GetKeys())
            {
                if (string.IsNullOrEmpty(key) || key.StartsWith("log4net:", StringComparison.OrdinalIgnoreCase) || Guid.TryParse(key, out _))
                {
                    continue;
                }

                if (properties[key] is { } value)
                {
                    log.SetAttribute($"property.{key}", value);
                }
            }
        }

        hub.Logger.CaptureLog(log);
    }
}
