using Sentry.Extensibility;

namespace Sentry.AspNet;

/// <summary>
/// HttpServerUtility extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryHttpServerUtilityExtensions
{
    /// <summary>
    /// Captures the last error from the given HttpServerUtility and sends it to Sentry.
    /// </summary>
    /// <param name="server">The HttpServerUtility that contains the last error.</param>
    /// <returns>A SentryId.</returns>
    public static SentryId CaptureLastError(this HttpServerUtility server) => server.CaptureLastError(HubAdapter.Instance);

    // for testing
    internal static SentryId CaptureLastError(this HttpServerUtility server, IHub hub)
    {
        if (server.GetLastError() is { } exception)
        {
            exception.SetSentryMechanism(
                "HttpApplication.Application_Error",
                "This exception was caught by the ASP.NET global error handler. " +
                "The web server likely returned a 5xx error code as a result of this exception.",
                handled: false);

            return hub.CaptureException(exception);
        }
        return SentryId.Empty;
    }
}
