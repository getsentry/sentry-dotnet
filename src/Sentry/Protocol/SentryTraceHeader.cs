using System;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry trace header.
    /// </summary>
    public class SentryTraceHeader
    {
        private readonly SentryId _traceId;
        private readonly SpanId _spanId;
        private readonly bool? _isSampled;

        /// <summary>
        /// Initializes an instance of <see cref="SentryTraceHeader"/>.
        /// </summary>
        public SentryTraceHeader(SentryId traceId, SpanId spanId, bool? isSampled)
        {
            _traceId = traceId;
            _spanId = spanId;
            _isSampled = isSampled;
        }

        /// <inheritdoc />
        public override string ToString() => _isSampled is {} isSampled
            ? $"{_traceId}-{_spanId}-{(isSampled ? 1 : 0)}"
            : $"{_traceId}-{_spanId}";

        /// <summary>
        /// Parses <see cref="SentryTraceHeader"/> from string.
        /// </summary>
        public static SentryTraceHeader Parse(string value)
        {
            var components = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (components.Length < 2)
            {
                throw new FormatException($"Invalid Sentry trace header: {value}.");
            }

            var traceId = SentryId.Parse(components[0]);
            var spanId = SpanId.Parse(components[1]);

            var isSampled = components.Length >= 3
                ? string.Equals(components[2], "1", StringComparison.OrdinalIgnoreCase)
                : (bool?)null;

            return new SentryTraceHeader(traceId, spanId, isSampled);
        }
    }
}
