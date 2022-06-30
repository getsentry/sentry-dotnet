using System.ComponentModel;
using System.Web;
using Sentry.Extensibility;
using Sentry.Protocol;

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
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "HttpApplication.Application_Error";
            return hub.CaptureException(exception);
        }
        return SentryId.Empty;
    }
}
