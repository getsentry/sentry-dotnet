using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;

namespace Sentry.Experimental;

internal static class SentryHubExtensions
{
    [Experimental(DiagnosticId.ExperimentalSentryLogs)]
    internal static int CaptureLog(this IHub hub, SentryLog log)
    {
        _ = hub.CaptureEnvelope(Envelope.FromLog(log));

        return default;
    }
}
