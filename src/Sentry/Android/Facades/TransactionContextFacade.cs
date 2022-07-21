using Sentry.Android.Extensions;

namespace Sentry.Android.Facades;

internal class TransactionContextFacade : ITransactionContext
{
    private readonly Java.TransactionContext _context;

    internal TransactionContextFacade(Java.TransactionContext context)
    {
        _context = context;
    }

    public string Name => _context.Name;

    public bool? IsParentSampled => _context.ParentSampled?.BooleanValue();

    public SpanId SpanId => _context.SpanId.ToSpanId();

    public SpanId? ParentSpanId => _context.ParentSpanId?.ToSpanId();

    public SentryId TraceId => _context.TraceId.ToSentryId();

    public string Operation => _context.Operation;

    public string? Description => _context.Description;

    public SpanStatus? Status => _context.Status?.ToSpanStatus();

    public bool? IsSampled => _context.Sampled?.BooleanValue();
}
