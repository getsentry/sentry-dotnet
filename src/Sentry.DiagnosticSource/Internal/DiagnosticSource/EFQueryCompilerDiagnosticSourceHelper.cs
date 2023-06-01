using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

// ReSharper disable once InconsistentNaming
internal class EFQueryCompilerDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFQueryCompilerDiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
        : base(hub, options, diagnosticSourceValue)
    {
    }

    protected override string Operation => "db.query.compile";
    protected override string Description => FilterNewLineValue(DiagnosticSourceValue) ?? string.Empty;

    /// <summary>
    /// Unfortunately there's nothing we can use as the correlation id for compiled query events, so we just return
    /// the first unfinished span for this operation.
    /// </summary>
    protected override ISpan? GetSpanReference(ITransaction transaction) =>
        transaction.Spans .FirstOrDefault(span => !span.IsFinished && span.Operation == Operation);

    /// <summary>
    /// We don't have a correlation id for compiled query events. This overload just prevents the base class from
    /// logging a debug message.
    /// </summary>
    protected override void SetSpanReference(ISpan span) { }
}
