using System.Net;
using System.Reflection.PortableExecutable;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry;

internal class SentryFailedRequestHandler : ISentryFailedRequestHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;

    public const string MechanismType = "SentryFailedRequestHandler";

    internal SentryFailedRequestHandler(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
    }

    public void HandleResponse(HttpResponseMessage response)
    {
        // Ensure reponse and request are not null
        if (response?.RequestMessage is null)
        {
            return;
        }

        // Don't capture if the option is disabled
        if (_options?.CaptureFailedRequests is false)
        {
            return;
        }

        // Don't capture events for successful requets
        if (_options?.FailedRequestStatusCodes.Any(range => range.Contains(response.StatusCode)) is false)
        {
            return;
        }

        // Ignore requests to the Sentry DSN
        var uri = response.RequestMessage.RequestUri;
        if (_options?.Dsn is { } dsn && new Uri(dsn).Host.Equals(uri?.Host, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Ignore requests that don't match the FailedRequestTargets
        var requestString = uri?.OriginalString ?? "";
        if (_options?.FailedRequestTargets.ContainsMatch(requestString) is false)
        {
            return;
        }

        // Capture the event
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            exception.SetSentryMechanism(MechanismType);

            var @event = new SentryEvent(exception);

            var sentryRequest = new Request
            {
                Url = uri?.AbsoluteUri,
                QueryString = uri?.Query,
                Method = response.RequestMessage.Method.Method,                
            };
            if (_options?.SendDefaultPii is true)
            {
                sentryRequest.Cookies = response.RequestMessage.Headers.GetCookies();
                sentryRequest.AddHeaders(response.RequestMessage.Headers);
            }

            var responseContext = new Response();
            responseContext.StatusCode = (short)response.StatusCode;
            responseContext.BodySize = response.Content?.Headers?.ContentLength;

            if (_options?.SendDefaultPii is true)
            {
                responseContext.Cookies = response.Headers.GetCookies();
                responseContext.AddHeaders(response.Headers);
            }

            @event.Request = sentryRequest;
            @event.Contexts[Response.Type] = responseContext;

            _hub.CaptureEvent(@event);
        }
    }
}
