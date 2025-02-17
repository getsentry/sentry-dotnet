using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// We know already, when starting a transaction, whether it's going to be sampled or not. When it's not sampled, we can
/// avoid lots of unecessary processing. The only thing we need to track is the number of spans that would have been
/// created (the client reports detailing discarded events includes this detail).
/// </summary>
internal sealed class UnsampledTransaction : NoOpTransaction
{
    // Although it's a little bit wasteful to create separate individual class instances here when all we're going to
    // report to sentry is the span count (in the client report), SDK users may refer to things like
    // `ITransaction.Spans.Count`, so we create an actual collection
    private readonly ConcurrentBag<ISpan> _spans = [];
    private readonly IHub _hub;
    private readonly ITransactionContext _context;
    private readonly SentryOptions? _options;

    public UnsampledTransaction(IHub hub, ITransactionContext context)
    {
        _hub = hub;
        _options = _hub.GetSentryOptions();
        _options?.LogDebug("Starting unsampled transaction");
        _context = context;
    }

    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

    public override IReadOnlyCollection<ISpan> Spans => _spans;

    public override SpanId SpanId => _context.SpanId;
    public override SentryId TraceId => _context.TraceId;
    public override bool? IsSampled => false;

    public override string Name
    {
        get => _context.Name;
        set { }
    }

    public override string Operation
    {
        get => _context.Operation;
        set { }
    }

    public override void Finish()
    {
        _options?.LogDebug("Finishing unsampled transaction");

        // Clear the transaction from the scope
        _hub.ConfigureScope(scope => scope.ResetTransaction(this));

        // Record the discarded events
        var spanCount = Spans.Count + 1; // 1 for each span + 1 for the transaction itself
        _options?.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.SampleRate, DataCategory.Transaction);
        _options?.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.SampleRate, DataCategory.Span, spanCount);

        _options?.LogDebug("Finished unsampled transaction");
    }

    public override void Finish(SpanStatus status) => Finish();

    public override void Finish(Exception exception, SpanStatus status) => Finish();

    public override void Finish(Exception exception) => Finish();

    /// <inheritdoc />
    public override SentryTraceHeader GetTraceHeader() => new(TraceId, SpanId, IsSampled);

    public override ISpan StartChild(string operation)
    {
        var span = new UnsampledSpan(this);
        _spans.Add(span);
        return span;
    }

    private  class UnsampledSpan(UnsampledTransaction transaction) : NoOpSpan
    {
        public override ISpan StartChild(string operation) => transaction.StartChild(operation);
    }
}
