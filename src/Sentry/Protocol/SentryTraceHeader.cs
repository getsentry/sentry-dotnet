using System;

namespace Sentry.Protocol
{
    public class SentryTraceHeader
    {
        private readonly SentryId _traceId;
        private readonly SentryId _spanId;
        private readonly bool? _isSampled;

        public SentryTraceHeader(SentryId traceId, SentryId spanId, bool? isSampled)
        {
            _traceId = traceId;
            _spanId = spanId;
            _isSampled = isSampled;
        }

        public override string ToString() => _isSampled is {} isSampled
            ? $"{_traceId}-{_spanId}-{(isSampled ? 1 : 0)}"
            : $"{_traceId}-{_spanId}";

        public static SentryTraceHeader Parse(string value)
        {
            var components = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (components.Length < 2)
            {
                throw new FormatException($"Invalid Sentry trace header: {value}.");
            }

            var traceId = SentryId.Parse(components[0]);
            var spanId = SentryId.Parse(components[1]);

            var isSampled = components.Length >= 3
                ? string.Equals(components[2], "1", StringComparison.OrdinalIgnoreCase)
                : (bool?)null;

            return new SentryTraceHeader(traceId, spanId, isSampled);
        }
    }
}
