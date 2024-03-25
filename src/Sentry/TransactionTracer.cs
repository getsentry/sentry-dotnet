namespace Sentry;

/// <summary>
/// Transaction tracer.
/// </summary>
public class TransactionTracer : SpanTracer
{
    /// <summary>
    /// Initializes an instance of <see cref="TransactionTracer"/>.
    /// </summary>
    public TransactionTracer(IHub hub, ITransactionContext context) : this(hub, context, null)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionTracer"/>.
    /// </summary>
    internal TransactionTracer(IHub hub, string name, string operation, TransactionNameSource nameSource = TransactionNameSource.Custom)
        : base(hub, name, operation, nameSource)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionTracer"/>.
    /// </summary>
    internal TransactionTracer(IHub hub, ITransactionContext context, TimeSpan? idleTimeout = null)
        : base(hub, context, idleTimeout)
    {
    }
}
