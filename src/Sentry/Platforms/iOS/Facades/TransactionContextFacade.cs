using Sentry.iOS.Extensions;

namespace Sentry.iOS.Facades;

internal class TransactionContextFacade : ITransactionContext
{
    private readonly SentryCocoa.SentryTransactionContext _context;

    internal TransactionContextFacade(SentryCocoa.SentryTransactionContext context)
    {
        _context = context;
    }

    public string Name => _context.Name;

    public bool? IsParentSampled => _context.ParentSampled.ToNullableBoolean();

    public SpanId SpanId => _context.SpanId.ToSpanId();

    public SpanId? ParentSpanId => _context.ParentSpanId?.ToSpanId();

    public SentryId TraceId => _context.TraceId.ToSentryId();

    public string Operation => _context.Operation;

    public string? Description => _context.Description;

    public TransactionNameSource? Source => _context.NameSource.ToCocoaTransactionNameSource();

    public SpanStatus? Status => _context.Status.ToSpanStatus();

    public bool? IsSampled => _context.Sampled.ToNullableBoolean();
}
