using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry.Protocol.Builders
{
    public class EnvelopeItemBuilder
    {
        private readonly Dictionary<string, object> _headers =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private byte[] _data = Array.Empty<byte>();

        public EnvelopeItemBuilder AddHeader(string key, object value)
        {
            _headers[key] = value;
            return this;
        }

        public EnvelopeItemBuilder SetData(byte[] data)
        {
            _data = data;
            return this;
        }

        public EnvelopeItemBuilder SetData(string data) => SetData(
            Encoding.UTF8.GetBytes(data)
        );

        public EnvelopeItem Build() => new EnvelopeItem(
            new EnvelopeHeaderCollection(_headers),
            new EnvelopePayload(_data)
        );
    }
}
