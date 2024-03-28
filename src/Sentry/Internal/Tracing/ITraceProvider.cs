namespace Sentry.Internal.Tracing;

internal interface ITraceProvider
{
    public ITracer GetTracer(string name, string? version = "");
}
