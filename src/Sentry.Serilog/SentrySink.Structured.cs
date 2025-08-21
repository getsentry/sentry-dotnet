using Sentry.Internal.Extensions;
using Serilog.Parsing;

namespace Sentry.Serilog;

internal sealed partial class SentrySink
{
    private static readonly SdkVersion Sdk = CreateSdkVersion();

    private void CaptureStructuredLog(IHub hub, LogEvent logEvent, string formatted, string? template)
    {
        var traceHeader = hub.GetTraceHeader() ?? SentryTraceHeader.Empty;
        GetStructuredLoggingParametersAndAttributes(logEvent, out var parameters, out var attributes);

        SentryLog log = new(logEvent.Timestamp, traceHeader.TraceId, logEvent.Level.ToSentryLogLevel(), formatted)
        {
            Template = template,
            Parameters = parameters,
            ParentSpanId = traceHeader.SpanId,
        };

        log.SetDefaultAttributes(_options, Sdk);

        foreach (var attribute in attributes)
        {
            log.SetAttribute(attribute.Key, attribute.Value);
        }

        hub.Logger.CaptureLog(log);
    }

    private static void GetStructuredLoggingParametersAndAttributes(LogEvent logEvent, out ImmutableArray<KeyValuePair<string, object>> parameters, out List<KeyValuePair<string, object>> attributes)
    {
        var propertyTokens = new List<PropertyToken>();
        foreach (var token in logEvent.MessageTemplate.Tokens)
        {
            if (token is PropertyToken property)
            {
                propertyTokens.Add(property);
            }
        }

        var @params = ImmutableArray.CreateBuilder<KeyValuePair<string, object>>();
        attributes = new List<KeyValuePair<string, object>>();

        foreach (var property in logEvent.Properties)
        {
            if (propertyTokens.Exists(prop => prop.PropertyName == property.Key))
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

    private static SdkVersion CreateSdkVersion()
    {
        return new SdkVersion
        {
            Name = SdkName,
            Version = NameAndVersion.Version,
        };
    }
}
