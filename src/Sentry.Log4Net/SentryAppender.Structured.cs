namespace Sentry.Log4Net;

public partial class SentryAppender
{
    private static void CaptureStructuredLog(IHub hub, SentryOptions options, LoggingEvent loggingEvent)
    {
        var level = loggingEvent.ToSentryLogLevel();
        if (level.HasValue)
        {
            DateTimeOffset timestamp = new(loggingEvent.TimeStampUtc);
            const string? template = null; // cannot get format-string from `log4net.Util.SystemStringFormat` via `LoggingEvent.MessageObject`
            var parameters = ImmutableArray<KeyValuePair<string, object>>.Empty; // cannot get arguments from `log4net.Util.SystemStringFormat` via `LoggingEvent.MessageObject`

            var log = SentryLog.Create(hub, timestamp, level.Value, loggingEvent.RenderedMessage, template, parameters);

            log.SetDefaultAttributes(options, Sdk);
            log.SetOrigin("auto.log.log4net");

            foreach (var property in loggingEvent.GetProperties())
            {
                if (property is DictionaryEntry { Key: string key, Value: { } value })
                {
                    if (key.Length != 0 && !key.StartsWith("log4net:", StringComparison.OrdinalIgnoreCase))
                    {
                        log.SetAttribute($"property.{key}", value);
                    }
                }
            }

            hub.Logger.CaptureLog(log);
        }
    }
}
