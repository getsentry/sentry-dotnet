using Sentry.Protocol;

namespace Sentry;

internal class SentryHttpFailedRequestHandler : ISentryFailedRequestHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;

    public const string MechanismType = "SentryFailedRequestHandler";

    internal SentryHttpFailedRequestHandler(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
    }

    internal static ISentryFailedRequestHandler Create(IHub hub, SentryOptions options)
        => new SentryHttpFailedRequestHandler(hub, options);

    public void HandleResponse(HttpResponseMessage response)
    {
        // Ensure request is not null
        if (response.RequestMessage is null)
        {
            return;
        }

        // Don't capture if the option is disabled
        if (!_options.CaptureFailedRequests)
        {
            return;
        }

        // Don't capture events for successful requests
        if (!_options.FailedRequestStatusCodes.Any(range => range.Contains(response.StatusCode)))
        {
            return;
        }

        // Ignore requests to the Sentry DSN
        var uri = response.RequestMessage.RequestUri;
        if (uri != null)
        {
            if (_options.Dsn is { } dsn && new Uri(dsn).Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Ignore requests that don't match the FailedRequestTargets
            var requestString = uri.ToString();
            if (!_options.FailedRequestTargets.ContainsMatch(requestString))
            {
                return;
            }
        }

#if NET5_0_OR_GREATER
        // Starting with .NET 5, the content and headers are guaranteed to not be null.
        var bodySize = response.Content.Headers.ContentLength;
#else
        // We have to get the content body size before calling EnsureSuccessStatusCode,
        // because older implementations of EnsureSuccessStatusCode disposes the content.
        // See https://github.com/dotnet/runtime/issues/24845

        // The ContentLength might be null (but that's ok).
        // See https://github.com/dotnet/runtime/issues/16162
        var bodySize = response.Content?.Headers?.ContentLength;
#endif

        // Capture the event
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            exception.SetSentryMechanism(MechanismType);

            var @event = new SentryEvent(exception);
            var hint = new Hint(HintTypes.HttpResponseMessage, response);

            var sentryRequest = new Request
            {
                QueryString = uri?.Query,
                Method = response.RequestMessage.Method.Method,
            };

            var responseContext = new Response {
                StatusCode = (short)response.StatusCode,
                BodySize = bodySize
            };

            if (!_options.SendDefaultPii)
            {
                sentryRequest.Url = uri?.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            else
            {
                sentryRequest.Url = uri?.AbsoluteUri;
                sentryRequest.Cookies = response.RequestMessage.Headers.GetCookies();
                sentryRequest.AddHeaders(response.RequestMessage.Headers);
                responseContext.Cookies = response.Headers.GetCookies();
                responseContext.AddHeaders(response.Headers);
            }

            @event.Request = sentryRequest;
            @event.Contexts[Response.Type] = responseContext;

            _hub.CaptureEvent(@event, hint);
        }
    }
}
