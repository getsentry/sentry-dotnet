using System;
using System.Collections.Generic;

namespace Sentry.Protocol.Builders
{
    public class EnvelopeBuilder
    {
        private readonly Dictionary<string, object> _headers =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private readonly List<EnvelopeItem> _items = new List<EnvelopeItem>();

        public EnvelopeBuilder WithHeader(string key, object value)
        {
            _headers[key] = value;
            return this;
        }

        public EnvelopeBuilder WithItem(EnvelopeItem item)
        {
            _items.Add(item);
            return this;
        }

        public EnvelopeBuilder WithItem(Action<EnvelopeItemBuilder> configure)
        {
            var builder = new EnvelopeItemBuilder();
            configure(builder);

            return WithItem(builder.Build());
        }

        public Envelope Build() => new Envelope(
            new EnvelopeHeaderCollection(_headers),
            new EnvelopeItemCollection(_items)
        );
    }
}
