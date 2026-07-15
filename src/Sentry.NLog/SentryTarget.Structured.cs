namespace Sentry.NLog;

public sealed partial class SentryTarget
{
    private static void CaptureStructuredLog(IHub hub, SentryOptions options, LogEventInfo logEvent)
    {
        var level = logEvent.Level.ToSentryLogLevel();
        if (level.HasValue)
        {
            DateTimeOffset timestamp = new(logEvent.TimeStamp);
            GetStructuredLoggingParametersAndAttributes(logEvent, out var parameters, out var attributes);

            var log = SentryLog.Create(hub, timestamp, level.Value, logEvent.FormattedMessage, logEvent.Message, parameters);

            log.SetDefaultAttributes(options, Sdk);
            log.SetOrigin("auto.log.nlog");

            if (logEvent.LoggerName is not null)
            {
                log.Attributes.SetAttribute("category.name", logEvent.LoggerName);
            }

            foreach (var attribute in attributes)
            {
                log.SetAttribute(attribute.Key, attribute.Value);
            }

            hub.Logger.CaptureLog(log);
        }
    }

    private static void GetStructuredLoggingParametersAndAttributes(LogEventInfo logEvent, out ImmutableArray<KeyValuePair<string, object>> parameters, out List<KeyValuePair<string, object>> attributes)
    {
        parameters = GetParameters(logEvent, out var parameterNames);
        attributes = new List<KeyValuePair<string, object>>();

        if (logEvent.HasProperties)
        {
            foreach (var property in logEvent.Properties)
            {
                if (property.Key is string key && !string.IsNullOrWhiteSpace(key) &&
                    property.Value is { } value &&
                    !parameterNames.Contains(key))
                {
                    attributes.Add(new KeyValuePair<string, object>($"property.{key}", value));
                }
            }
        }
    }

    private static ImmutableArray<KeyValuePair<string, object>> GetParameters(LogEventInfo logEvent, out HashSet<string> parameterNames)
    {
        var parameters = logEvent.MessageTemplateParameters;

        if (parameters.Count == 0)
        {
            parameterNames = new HashSet<string>();
            return ImmutableArray<KeyValuePair<string, object>>.Empty;
        }

        // The HashSet<T> capacity constructor is unavailable on netstandard2.0 and net462 (added in net472).
#if NETSTANDARD2_0 || NET462
        parameterNames = new HashSet<string>();
#else
        parameterNames = new HashSet<string>(parameters.Count);
#endif

        var @params = ImmutableArray.CreateBuilder<KeyValuePair<string, object>>(parameters.Count);

        var index = 0;
        foreach (var parameter in parameters)
        {
            // Unnamed holes (e.g. `{}`) have an empty name. Fall back to the positional index so that
            // multiple unnamed holes don't collide on the same `sentry.message.parameter.` attribute key.
            var name = string.IsNullOrEmpty(parameter.Name)
                ? index.ToString(CultureInfo.InvariantCulture)
                : parameter.Name;

            parameterNames.Add(name);
            @params.Add(new KeyValuePair<string, object>(name, parameter.Value));
            index++;
        }

        return @params.DrainToImmutable();
    }
}
