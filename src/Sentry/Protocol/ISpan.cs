using System;
using System.Collections.Generic;

namespace Sentry.Protocol
{
    public interface ISpan
    {
        SentryId SpanId { get; }

        SentryId? ParentSpanId { get; }

        SentryId TraceId { get; }

        DateTimeOffset StartTimestamp { get; }

        DateTimeOffset? EndTimestamp { get; }

        string Operation { get; }

        string? Description { get; set; }

        SpanStatus? Status { get; }

        bool IsSampled { get; set; }

        IReadOnlyDictionary<string, string> Tags { get; }

        IReadOnlyDictionary<string, object> Data { get; }

        ISpan StartChild(string operation);

        void Finish(SpanStatus status = SpanStatus.Ok);
    }
}
