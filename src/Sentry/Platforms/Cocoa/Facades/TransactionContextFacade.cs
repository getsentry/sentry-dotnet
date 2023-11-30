using Sentry.Cocoa.Extensions;

namespace Sentry.Cocoa.Facades;

internal class TransactionContextFacade : ITransactionContext
{
    private readonly CocoaSdk.SentryTransactionContext _context;

    internal TransactionContextFacade(CocoaSdk.SentryTransactionContext context)
    {
        _context = context;
    }

    public string Name => _context.Name;

    public TransactionNameSource NameSource => _context.NameSource.ToTransactionNameSource();

    public bool? IsParentSampled => _context.ParentSampled.ToNullableBoolean();

    public SpanId SpanId => _context.SpanId.ToSpanId();

    public SpanId? ParentSpanId => _context.ParentSpanId?.ToSpanId();

    public SentryId TraceId => _context.TraceId.ToSentryId();

    public string Operation => _context.Operation;

    public string Description => _context.Description;

    // Note: SentrySpanContext.Status was removed from the Cocoa SDK in 8.0.0
    public SpanStatus? Status => null; // _context.Status.ToSpanStatus();

    public bool? IsSampled => _context.Sampled.ToNullableBoolean();
}
