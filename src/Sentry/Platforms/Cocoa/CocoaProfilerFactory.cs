using Sentry.Cocoa.Extensions;
using Sentry.Internal;

namespace Sentry.Cocoa;

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
        return new CocoaProfiler(_options, startTime, tracer.TraceId, traceId);
    }
}
