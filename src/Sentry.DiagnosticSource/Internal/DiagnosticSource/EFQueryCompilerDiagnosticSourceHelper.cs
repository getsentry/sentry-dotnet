namespace Sentry.Internal.DiagnosticSource;

internal class EFQueryCompilerDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFQueryCompilerDiagnosticSourceHelper(IHub hub, SentryOptions options) : base(hub, options)
    {
    }

    protected override string Operation => "db.query.compile";

    protected override string GetDescription(object? diagnosticSourceValue) => FilterNewLineValue(diagnosticSourceValue) ?? string.Empty;

    /// <summary>
    /// We don't have a correlation id for compiled query events. We just return the first unfinished query compile span.
    /// </summary>
    protected override ISpan? GetSpanReference(ITransactionTracer transaction, object? diagnosticSourceValue) =>
        transaction.Spans .FirstOrDefault(span => !span.IsFinished && span.Operation == Operation);

    protected override void SetSpanReference(ISpan span, object? diagnosticSourceValue)
    {
        // We don't have a correlation id for compiled query events.
    }
}
