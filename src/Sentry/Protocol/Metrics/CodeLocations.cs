using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Represents a collection of code locations.
/// </summary>
internal class CodeLocations(long timestamp, IReadOnlyDictionary<MetricResourceIdentifier, SentryStackFrame> locations)
    : ISentryJsonSerializable
{
    /// <summary>
    /// Uniquely identifies a code location using the number of seconds since the UnixEpoch, as measured at the start
    /// of the day when the code location was recorded.
    /// </summary>
    public long Timestamp => timestamp;

    /// <inheritdoc cref="ISentryJsonSerializable.WriteTo"/>
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteNumber("timestamp", Timestamp);

        var mapping = locations.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp =>
            {
                var loc = kvp.Value;
                loc.IsCodeLocation = true;
                return loc;
            });

        writer.WritePropertyName("mapping");
        writer.WriteStartObject();
        foreach (var (mri, loc) in mapping)
        {
            // The protocol supports multiple locations per MRI but currently the Sentry Relay will discard all but the
            // first, so even though we only capture a single location we send it through as an array.
            // See: https://discord.com/channels/621778831602221064/1184350202774163556/1185010167369170974
            writer.WriteArray(mri, new[] { loc }, logger);
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
