using Serilog.Parsing;
using Sentry.Internal.Extensions;

namespace Sentry.Serilog;

internal sealed partial class SentrySink
{
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

        var scope = hub.GetScope();
        log.SetDefaultAttributes(_options, scope?.Sdk ?? SdkVersion.Instance);

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
                if (TryGetLogEventProperty(property, out var parameter))
                {
                    @params.Add(parameter);
                }
            }
            else
            {
                if (TryGetLogEventProperty(property, out var attribute))
                {
                    attributes.Add(new KeyValuePair<string, object>($"property.{attribute.Key}", attribute.Value));
                }
            }
        }

        parameters = @params.DrainToImmutable();
        return;

        static bool TryGetLogEventProperty(KeyValuePair<string, LogEventPropertyValue> property, out KeyValuePair<string, object> value)
        {
            if (property.Value is ScalarValue scalarValue)
            {
                if (scalarValue.Value is not null)
                {
                    value = new KeyValuePair<string, object>(property.Key, scalarValue.Value);
                    return true;
                }
            }
            else if (property.Value is StructureValue structureValue)
            {
                foreach (var prop in structureValue.Properties)
                {
                    if (LogEventProperty.IsValidName(prop.Name))
                    {
                        if (prop.Value is ScalarValue scalarProperty)
                        {
                            if (scalarProperty.Value is not null)
                            {
                                value = new KeyValuePair<string, object>($"{property.Key}.{prop.Name}", scalarProperty.Value);
                                return true;
                            }
                        }
                        else
                        {
                            value = new KeyValuePair<string, object>($"{property.Key}.{prop.Name}", prop.Value);
                            return true;
                        }
                    }
                }
            }
            else if (!property.Value.IsNull())
            {
                value = new KeyValuePair<string, object>(property.Key, property.Value);
                return true;
            }

            value = default;
            return false;
        }
    }
}
