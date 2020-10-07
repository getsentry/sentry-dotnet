using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry.Protocol.Builders
{
    /// <summary>
    /// Builder for <see cref="EnvelopeItemBuilder"/>.
    /// </summary>
    public class EnvelopeItemBuilder
    {
        private readonly Dictionary<string, object> _headers =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private byte[] _data = Array.Empty<byte>();

        /// <summary>
        /// Adds the specified header to the item.
        /// </summary>
        public EnvelopeItemBuilder AddHeader(string key, object value)
        {
            _headers[key] = value;
            return this;
        }

        /// <summary>
        /// Sets the payload data to the specified byte array.
        /// </summary>
        public EnvelopeItemBuilder SetData(byte[] data)
        {
            _data = data;
            return this;
        }

        /// <summary>
        /// Sets the payload data to the specified string.
        /// </summary>
        public EnvelopeItemBuilder SetData(string data) => SetData(
            Encoding.UTF8.GetBytes(data)
        );

        /// <summary>
        /// Builds the resulting <see cref="EnvelopeItem"/>.
        /// </summary>
        public EnvelopeItem Build() => new EnvelopeItem(
            new EnvelopeHeaderCollection(_headers),
            new EnvelopePayload(_data)
        );
    }
}
