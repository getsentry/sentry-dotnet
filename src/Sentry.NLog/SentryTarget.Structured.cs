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

#if NETSTANDARD2_1_OR_GREATER || NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        parameterNames = new HashSet<string>(parameters.Count);
#else
        parameterNames = new HashSet<string>();
#endif

        var @params = ImmutableArray.CreateBuilder<KeyValuePair<string, object>>(parameters.Count);

        foreach (var parameter in parameters)
        {
            parameterNames.Add(parameter.Name);
            @params.Add(new KeyValuePair<string, object>(parameter.Name, parameter.Value));
        }

        return @params.DrainToImmutable();
    }
}
