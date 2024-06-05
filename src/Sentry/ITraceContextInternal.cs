using Sentry.Protocol;

namespace Sentry;

/// <summary>
///  Internal interface defining members to be added to the TraceContexts for the next major release
///  (since adding members to interfaces is a breaking change).
/// </summary>
/// <remarks>
/// TODO: Remove this interface in the next major version.
/// </remarks>
internal interface ITraceContextInternal
{
    /// <summary>
    /// Specifies the origin of the trace. If no origin is set then the trace origin is assumed to be "manual".
    /// </summary>
    /// <remarks>
    /// See https://develop.sentry.dev/sdk/performance/trace-origin/ for more information.
    /// </remarks>
    Origin? Origin { get; }
}

/// <summary>
/// Some internal extensions that we can remove once we deprecate the ITraceContextInternal interface.
/// </summary>
internal static class ITraceContextInternalExtensions
{
    public static void SetOrigin(this ISpan span, Origin origin)
    {
        switch (span)
        {
            case SpanTracer spanTracer:
                spanTracer.Origin = origin;
                break;
            case TransactionTracer transactionTracer:
                transactionTracer.Contexts.Trace.Origin = origin;
                break;
        }
    }
}
