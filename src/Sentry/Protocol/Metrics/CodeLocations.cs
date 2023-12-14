using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

internal class CodeLocations(long timestamp, Dictionary<MetricResourceIdentifier, SentryStackFrame> locations)
    : IJsonSerializable
{
    /// <summary>
    /// Uniquely identifies a code location using the number of seconds since the UnixEpoch, as measured at the start
    /// of the day when the code location was recorded.
    /// </summary>
    public long Timestamp { get; set; } = timestamp;

    public Dictionary<MetricResourceIdentifier, SentryStackFrame> Locations { get; set; } = locations;

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteNumber("timestamp", Timestamp);

        var mapping = Locations.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp =>
            {
                var loc = kvp.Value;
                loc.IsCodeLocation = true;
                return loc;
            });
        writer.WriteDictionary("mapping", mapping, logger);

        writer.WriteEndObject();
    }
}
