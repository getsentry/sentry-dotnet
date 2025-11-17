using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// We know already, when starting a transaction, whether it's going to be sampled or not. When it's not sampled, we can
/// avoid lots of unnecessary processing. The only thing we need to track is the number of spans that would have been
/// created (the client reports detailing discarded events includes this detail).
/// </summary>
internal sealed class UnsampledTransaction : NoOpTransaction
{
    // Although it's a little bit wasteful to create separate individual class instances here when all we're going to
    // report to sentry is the span count (in the client report), SDK users may refer to things like
    // `ITransaction.Spans.Count`, so we create an actual collection
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private readonly ConcurrentBag<ISpan> _spans = [];
#else
    private ConcurrentBag<ISpan> _spans = [];
#endif

    private readonly IHub _hub;
    private readonly ITransactionContext _context;
    private readonly SentryOptions? _options;
    private InterlockedBoolean _isFinished;

    public UnsampledTransaction(IHub hub, ITransactionContext context)
    {
        _hub = hub;
        _options = _hub.GetSentryOptions();
        _options?.LogDebug("Starting unsampled transaction");
        _context = context;
    }

    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

    public override bool IsFinished => _isFinished;

    public override IReadOnlyCollection<ISpan> Spans => _spans;

    public override SpanId SpanId => _context.SpanId;

    public override SentryId TraceId => _context.TraceId;

    public override bool? IsSampled => false;

    public double? SampleRate { get; set; }

    public double? SampleRand { get; set; }

    public DiscardReason? DiscardReason { get; set; }

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
        // Ensure the transaction is really cleared from the scope
        // See: https://github.com/getsentry/sentry-dotnet/issues/4198
        if (_isFinished.Exchange(true))
        {
            return;
        }

        _options?.LogDebug("Finishing unsampled transaction");

        // Clear the transaction from the scope and regenerate the Propagation Context, so new events don't have a
        // trace context that is "older" than the transaction that just finished
        _hub.ConfigureScope(static (scope, transactionTracer) =>
        {
            scope.ResetTransaction(transactionTracer);
            scope.SetPropagationContext(new SentryPropagationContext());
        }, this);

        // Record the discarded events
        var spanCount = Spans.Count + 1; // 1 for each span + 1 for the transaction itself
        var discardReason = DiscardReason ?? Internal.DiscardReason.SampleRate;
        _options?.ClientReportRecorder.RecordDiscardedEvent(discardReason, DataCategory.Transaction);
        _options?.ClientReportRecorder.RecordDiscardedEvent(discardReason, DataCategory.Span, spanCount);

        _options?.LogDebug("Finished unsampled transaction");

        // Release tracked spans
        ReleaseSpans();
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

    public ISpan StartChild(string operation, SpanId spanId)
    {
        var span = new UnsampledSpan(this, spanId);
        _spans.Add(span);
        return span;
    }

    private void ReleaseSpans()
    {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        _spans.Clear();
#else
        _spans = [];
#endif
    }
}
