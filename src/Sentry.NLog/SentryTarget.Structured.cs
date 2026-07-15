namespace Sentry.NLog;

public sealed partial class SentryTarget
{
    private static void CaptureStructuredLog(IHub hub, SentryOptions options, LogEventInfo logEvent)
    {
        if (logEvent.Level.ToSentryLogLevel() is not { } level)
        {
            return;
        }

        DateTimeOffset timestamp = new(logEvent.TimeStamp);
        GetStructuredLoggingParametersAndAttributes(logEvent, out var parameters, out var attributes);

        var log = SentryLog.Create(hub, timestamp, level, logEvent.FormattedMessage, logEvent.Message, parameters);

        var scope = hub.GetScope();
        log.SetDefaultAttributes(options, scope, Sdk);
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

    private static void GetStructuredLoggingParametersAndAttributes(LogEventInfo logEvent, out ImmutableArray<KeyValuePair<string, object>> parameters, out List<KeyValuePair<string, object>> attributes)
    {
        parameters = GetParameters(logEvent, out var parameterNames);
        attributes = [];

        if (!logEvent.HasProperties)
        {
            return;
        }

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
            // NLog allows passing unnamed holes (e.g. `{}`) - parameters with no names.
            // To prevent a collision on the attribute key when multiple unnamed holes are present, we fall back to the
            // positional index of the parameter in these cases (matching the behaviour of MEL).
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
