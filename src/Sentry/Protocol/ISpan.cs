using System;
using System.Collections.Generic;

namespace Sentry.Protocol
{
    public interface ISpan
    {
        SentryId SpanId { get; }

        SentryId? ParentSpanId { get; }

        SentryId TraceId { get; set; }

        DateTimeOffset StartTimestamp { get; set; }

        DateTimeOffset EndTimestamp { get; set; }

        string? Operation { get; set; }

        string? Description { get; set; }

        SpanStatus? Status { get; set; }

        bool IsSampled { get; set; }

        IReadOnlyDictionary<string, string> Tags { get; }

        IReadOnlyDictionary<string, object> Data { get; }

        ISpan StartChild();

        void Finish();
    }
}
