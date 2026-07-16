namespace Sentry.Log4Net;

public partial class SentryAppender
{
    private static void CaptureStructuredLog(IHub hub, SentryOptions options, LoggingEvent loggingEvent)
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

        if (loggingEvent.LoggerName is { } loggerName)
        {
            log.SetAttribute("category.name", loggerName);
        }

        foreach (var property in loggingEvent.GetProperties())
        {
            if (property is DictionaryEntry { Key: string key, Value: { } value })
            {
                if (key.Length != 0 && !key.StartsWith("log4net:", StringComparison.OrdinalIgnoreCase) && !Guid.TryParse(key, out _))
                {
                    log.SetAttribute($"property.{key}", value);
                }
            }
        }

        hub.Logger.CaptureLog(log);
    }
}
