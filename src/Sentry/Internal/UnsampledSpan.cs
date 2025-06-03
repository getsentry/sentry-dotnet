namespace Sentry.Internal;

internal sealed class UnsampledSpan(UnsampledTransaction transaction, SpanId? spanId = null) : NoOpSpan
{
    public override bool? IsSampled => false;
    public override SpanId SpanId { get; } = spanId ?? SpanId.Empty;
    internal UnsampledTransaction Transaction => transaction;
    public override ISpan StartChild(string operation) => transaction.StartChild(operation);
}
