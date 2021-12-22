using System.Web;
using Sentry.Protocol;

namespace Sentry.AspNet;
/// <summary>
/// HttpServerUtility extensions.
/// </summary>
public static class SentryHttpServerUtilityExtensions
{
    /// <summary>
    /// Caoture the last error from the given HttpServerUtility and send to Sentry.
    /// </summary>
    /// <param name="server">The HttpServerUtility that contains the last error.</param>
    /// <param name="hub">(optional) The Hub that will capture the exception.</param>
    /// <returns>A SentryId.</returns>
    public static SentryId CaptureLastError(this HttpServerUtility server, IHub? hub = null)
    {
        if (server.GetLastError() is { } exception)
        {
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "HttpApplication.Application_Error";
            return hub?.CaptureException(exception) ?? SentrySdk.CaptureException(exception);
        }
        return SentryId.Empty;
    }
}
