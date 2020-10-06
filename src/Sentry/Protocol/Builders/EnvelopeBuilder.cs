using System;
using System.Collections.Generic;

namespace Sentry.Protocol.Builders
{
    public class EnvelopeBuilder
    {
        private readonly Dictionary<string, object> _headers =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private readonly List<EnvelopeItem> _items = new List<EnvelopeItem>();

        public EnvelopeBuilder AddHeader(string key, object value)
        {
            _headers[key] = value;
            return this;
        }

        public EnvelopeBuilder AddItem(EnvelopeItem item)
        {
            _items.Add(item);
            return this;
        }

        public EnvelopeBuilder AddItem(Action<EnvelopeItemBuilder> configure)
        {
            var builder = new EnvelopeItemBuilder();
            configure(builder);

            return AddItem(builder.Build());
        }

        public Envelope Build() => new Envelope(
            new EnvelopeHeaderCollection(_headers),
            new EnvelopeItemCollection(_items)
        );
    }
}
