using System;
using System.Collections.Generic;

namespace Sentry.Protocol.Builders
{
    public class EnvelopeItemBuilder
    {
        private readonly Dictionary<string, object> _headers =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private byte[] _data = Array.Empty<byte>();

        public EnvelopeItemBuilder WithHeader(string key, object value)
        {
            _headers[key] = value;
            return this;
        }

        public EnvelopeItemBuilder SetData(byte[] data)
        {
            _data = data;
            return this;
        }

        public EnvelopeItem Build() => new EnvelopeItem(
            new EnvelopeHeaderCollection(_headers),
            new EnvelopePayload(_data)
        );
    }
}
