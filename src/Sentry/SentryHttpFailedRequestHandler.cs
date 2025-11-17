using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry;

internal class SentryHttpFailedRequestHandler : SentryFailedRequestHandler
{
    public const string MechanismType = "SentryHttpFailedRequestHandler";

    internal SentryHttpFailedRequestHandler(IHub hub, SentryOptions options)
    : base(hub, options)
    {
    }

    protected internal override void DoEnsureSuccessfulResponse([NotNull] HttpRequestMessage request, [NotNull] HttpResponseMessage response)
    {
        // Don't capture events for successful requests
        if (!Options.FailedRequestStatusCodes.ContainsStatusCode(response.StatusCode))
        {
            return;
        }

        // Capture the event

        var statusCode = (int)response.StatusCode;
        // Match behavior of HttpResponseMessage.EnsureSuccessStatusCode
        if (statusCode >= 200 && statusCode <= 299)
        {
            return;
        }

        var exception = new HttpRequestException(
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Response status code does not indicate success: {0}",
                statusCode)
            );

#if NET6_0_OR_GREATER
        // Add a full stack trace into the exception to improve Issue grouping,
        // see https://github.com/getsentry/sentry-dotnet/issues/3582
        ExceptionDispatchInfo.SetCurrentStackTrace(exception);
#else
        // Where SetRemoteStackTrace is not available, throw and catch to get a basic stack trace
        try
        {
            throw exception;
        }
        catch (HttpRequestException ex)
        {
            exception = ex;
        }
#endif

        exception.SetSentryMechanism(MechanismType);

        var @event = new SentryEvent(exception);
        var hint = new SentryHint(HintTypes.HttpResponseMessage, response);

        var uri = response.RequestMessage?.RequestUri;
        var sentryRequest = new SentryRequest
        {
            QueryString = uri?.Query,
            Method = response.RequestMessage?.Method.Method.ToUpperInvariant()
        };

        var responseContext = new Response
        {
            StatusCode = (short)response.StatusCode,
#if NET5_0_OR_GREATER
            // Starting with .NET 5, the content and headers are guaranteed to not be null.
            BodySize = response.Content.Headers.ContentLength
#else
            // The ContentLength might be null (but that's ok).
            // See https://github.com/dotnet/runtime/issues/16162
            BodySize = response.Content?.Headers?.ContentLength
#endif
        };

        if (!Options.SendDefaultPii)
        {
            sentryRequest.Url = uri?.HttpRequestUrl();
        }
        else
        {
            sentryRequest.Url = uri?.AbsoluteUri;
            sentryRequest.Cookies = request.Headers.GetCookies();
            sentryRequest.AddHeaders(request.Headers);
            responseContext.Cookies = response.Headers.GetCookies();
            responseContext.AddHeaders(response.Headers);
        }

        @event.Request = sentryRequest;
        @event.Contexts[Response.Type] = responseContext;

        Hub.CaptureEvent(@event, hint: hint);
    }
}
