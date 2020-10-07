using System;
using System.Collections.Generic;
using Sentry.Internal;

namespace Sentry.Protocol.Builders
{
    /// <summary>
    /// Builder for <see cref="Envelope"/>.
    /// </summary>
    public class EnvelopeBuilder
    {
        private readonly Dictionary<string, object> _headers =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private readonly List<EnvelopeItem> _items = new List<EnvelopeItem>();

        /// <summary>
        /// Adds the specified header to the envelope.
        /// </summary>
        public EnvelopeBuilder AddHeader(string key, object value)
        {
            _headers[key] = value;
            return this;
        }

        /// <summary>
        /// Adds the specified item to the envelope.
        /// </summary>
        public EnvelopeBuilder AddItem(EnvelopeItem item)
        {
            _items.Add(item);
            return this;
        }

        /// <summary>
        /// Adds the specified item to the envelope.
        /// </summary>
        public EnvelopeBuilder AddItem(Action<EnvelopeItemBuilder> configure)
        {
            var builder = new EnvelopeItemBuilder();
            configure(builder);

            return AddItem(builder.Build());
        }

        // https://develop.sentry.dev/sdk/envelopes/#event
        /// <summary>
        /// Adds the specified event to the envelope.
        /// </summary>
        public EnvelopeBuilder AddEventItem(SentryEvent @event)
        {
            AddHeader("event_id", @event.EventId.ToString());

            var eventJson = JsonSerializer.SerializeObject(@event);

            AddItem(i => i
                .AddHeader("type", "event")
                .AddHeader("length", eventJson.Length)
                .SetData(JsonSerializer.SerializeObject(@event)));

            return this;
        }

        /// <summary>
        /// Builds the resulting <see cref="Envelope"/>.
        /// </summary>
        public Envelope Build() => new Envelope(
            new EnvelopeHeaderCollection(_headers),
            new EnvelopeItemCollection(_items)
        );
    }
}
