using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal
{
    internal class ClientReport : IJsonSerializable
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
            foreach (var ((category, reason), value) in DiscardedEvents.Where(x=> x.Value > 0))
            {
                writer.WriteStartObject();
                writer.WriteString("reason", reason);
                writer.WriteString("category", category);
                writer.WriteNumber("quantity", value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
