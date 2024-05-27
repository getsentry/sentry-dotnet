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
        if (!Options.FailedRequestStatusCodes.Any(range => range.Contains(response.StatusCode)))
        {
            return;
        }

        // Capture the event
        try
        {
#if NET5_0_OR_GREATER
            response.EnsureSuccessStatusCode();
#else
            // Use our own implementation of EnsureSuccessStatusCode because older implementations of
            // EnsureSuccessStatusCode disposes the content.
            // See https://github.com/dotnet/runtime/issues/24845
            response.StatusCode.EnsureSuccessStatusCode();
#endif
        }
        catch (HttpRequestException exception)
        {
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
}
