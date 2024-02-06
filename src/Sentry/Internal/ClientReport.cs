using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class ClientReport : ISentryJsonSerializable
{
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyDictionary<DiscardReasonWithCategory, int> DiscardedEvents { get; }

    public ClientReport(DateTimeOffset timestamp,
        IReadOnlyDictionary<DiscardReasonWithCategory, int> discardedEvents)
    {
        Timestamp = timestamp;
        DiscardedEvents = discardedEvents;
    }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("timestamp", Timestamp);

        writer.WriteStartArray("discarded_events");

        // filter out empty counters, and sort the counters to allow for deterministic testing
        var discardedEvents = DiscardedEvents
            .Where(x => x.Value > 0)
            .OrderBy(x => x.Key.Reason)
            .ThenBy(x => x.Key.Category);

        foreach (var item in discardedEvents)
        {
            writer.WriteStartObject();
            writer.WriteString("reason", item.Key.Reason);
            writer.WriteString("category", item.Key.Category);
            writer.WriteNumber("quantity", item.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses <see cref="ClientReport"/> from <paramref name="json"/>.
    /// </summary>
    public static ClientReport FromJson(JsonElement json)
    {
        var timestamp = json.GetProperty("timestamp").GetDateTimeOffset();
        var discardedEvents = json.GetProperty("discarded_events").EnumerateArray()
            .Select(x => new
            {
                Reason = x.GetProperty("reason").GetString()!,
                Category = x.GetProperty("category").GetString()!,
                Quantity = x.GetProperty("quantity").GetInt32()
            })
            .ToDictionary(
                x => new DiscardReasonWithCategory(x.Reason, x.Category),
                x => x.Quantity);

        return new ClientReport(timestamp, discardedEvents);
    }
}
