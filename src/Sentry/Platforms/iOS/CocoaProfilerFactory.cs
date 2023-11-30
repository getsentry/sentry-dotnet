using Sentry.Internal;
using Sentry.iOS.Extensions;

namespace Sentry.iOS;

internal class CocoaProfilerFactory : ITransactionProfilerFactory
{
    private readonly SentryOptions _options;

    internal CocoaProfilerFactory(SentryOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public ITransactionProfiler? Start(ITransactionTracer tracer, CancellationToken cancellationToken)
    {
        var traceId = tracer.TraceId.ToCocoaSentryId();
        var startTime = SentryCocoaHybridSdk.StartProfilerForTrace(traceId);
        if (startTime == 0)
        {
            return null;
        }
        return new CocoaProfiler(_options, startTime, tracer.TraceId, traceId);
    }
}
