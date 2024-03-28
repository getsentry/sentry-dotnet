namespace Sentry.Internal.Tracing;

internal class SentryTraceProvider(IHub hub) : ITraceProvider
{
    private readonly SentryTracer _tracer = new(hub);

    // Sentry doesn't have the same concept of "Tracers" as the DiagnosticSource classes do, so we always return the
    // same tracer for Sentry... it's just a wrapper around the Hub which is actually what Starts and Stops spans when
    // using Sentry tracing.
    public ITracer GetTracer(string name, string? version = "") => _tracer;
}
