namespace Sentry.Tunnel;

/// <summary>
/// Middleware that can forward Sentry envelopes.
/// </summary>
/// <seealso href="https://docs.sentry.io/platforms/javascript/troubleshooting/#dealing-with-ad-blockers"/>
[Obsolete("The functionality from Sentry.Tunnel has been moved into Sentry.AspNetCore. Remove the Sentry.Tunnel NuGet and ensure the Sentry.AspNetCore NuGet is referenced.")]
public class SentryTunnelMiddleware :
    Sentry.AspNetCore.SentryTunnelMiddleware
{
    /// <summary>
    /// Middleware that can forward Sentry envelopes.
    /// </summary>
    /// <seealso href="https://docs.sentry.io/platforms/javascript/troubleshooting/#dealing-with-ad-blockers"/>
    public SentryTunnelMiddleware(string[] allowedHosts) :
        base(allowedHosts)
    {
    }
}
