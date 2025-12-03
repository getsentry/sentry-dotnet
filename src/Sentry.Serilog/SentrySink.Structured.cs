using Sentry.Internal.Extensions;
using Serilog.Parsing;

namespace Sentry.Serilog;

internal sealed partial class SentrySink
{
    private static void CaptureStructuredLog(IHub hub, SentryOptions options, LogEvent logEvent, string formatted, string? template)
    {
        SentryLog.GetTraceIdAndSpanId(hub, out var traceId, out var spanId);
        GetStructuredLoggingParametersAndAttributes(logEvent, out var parameters, out var attributes);

        SentryLog log = new(logEvent.Timestamp, traceId, logEvent.Level.ToSentryLogLevel(), formatted)
        {
            Template = template,
            Parameters = parameters,
            SpanId = spanId,
        };

        log.SetDefaultAttributes(options, hub.GetScope(), Sdk);
        log.SetOrigin("auto.log.serilog");

        foreach (var attribute in attributes)
        {
            log.SetAttribute(attribute.Key, attribute.Value);
        }

        hub.Logger.CaptureLog(log);
    }

    private static void GetStructuredLoggingParametersAndAttributes(LogEvent logEvent, out ImmutableArray<KeyValuePair<string, object>> parameters, out List<KeyValuePair<string, object>> attributes)
    {
        var propertyNames = new HashSet<string>();
        foreach (var token in logEvent.MessageTemplate.Tokens)
        {
            if (token is PropertyToken property)
            {
                propertyNames.Add(property.PropertyName);
            }
        }

        var @params = ImmutableArray.CreateBuilder<KeyValuePair<string, object>>();
        attributes = new List<KeyValuePair<string, object>>();

        foreach (var property in logEvent.Properties)
        {
            if (propertyNames.Contains(property.Key))
            {
                foreach (var parameter in GetLogEventProperties(property))
                {
                    @params.Add(parameter);
                }
            }
            else
            {
                foreach (var attribute in GetLogEventProperties(property))
                {
                    attributes.Add(new KeyValuePair<string, object>($"property.{attribute.Key}", attribute.Value));
                }
            }
        }

        parameters = @params.DrainToImmutable();
        return;

        static IEnumerable<KeyValuePair<string, object>> GetLogEventProperties(KeyValuePair<string, LogEventPropertyValue> property)
        {
            if (property.Value is ScalarValue scalarValue)
            {
                if (scalarValue.Value is not null)
                {
                    yield return new KeyValuePair<string, object>(property.Key, scalarValue.Value);
                }
            }
            else if (property.Value is SequenceValue sequenceValue)
            {
                if (sequenceValue.Elements.Count != 0)
                {
                    yield return new KeyValuePair<string, object>(property.Key, sequenceValue.ToString());
                }
            }
            else if (property.Value is DictionaryValue dictionaryValue)
            {
                if (dictionaryValue.Elements.Count != 0)
                {
                    yield return new KeyValuePair<string, object>(property.Key, dictionaryValue.ToString());
                }
            }
            else if (property.Value is StructureValue structureValue)
            {
                foreach (var prop in structureValue.Properties)
                {
                    if (LogEventProperty.IsValidName(prop.Name))
                    {
                        yield return new KeyValuePair<string, object>($"{property.Key}.{prop.Name}", prop.Value.ToString());
                    }
                }
            }
            else if (!property.Value.IsNull())
            {
                yield return new KeyValuePair<string, object>(property.Key, property.Value);
            }
        }
    }
}
