namespace Sentry;

internal abstract class SentryFailedRequestHandler : ISentryFailedRequestHandler
{
    protected IHub Hub { get; }
    protected SentryOptions Options { get; }

    internal SentryFailedRequestHandler(IHub hub, SentryOptions options)
    {
        Hub = hub;
        Options = options;
    }

    protected internal abstract void DoEnsureSuccessfulResponse(HttpRequestMessage request, HttpResponseMessage response);

    public void HandleResponse(HttpResponseMessage response)
    {
        // Ensure request is not null
        if (response.RequestMessage is null)
        {
            return;
        }

        // Don't capture if the option is disabled
        if (!Options.CaptureFailedRequests)
        {
            return;
        }

        // Ignore requests to the Sentry DSN
        var uri = response.RequestMessage.RequestUri;
        if (uri != null)
        {
            if (Options.Dsn is { } dsn && new Uri(dsn).Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Ignore requests that don't match the FailedRequestTargets
            var requestString = uri.ToString();
            if (!Options.FailedRequestTargets.MatchesSubstringOrRegex(requestString))
            {
                return;
            }
        }

        DoEnsureSuccessfulResponse(response.RequestMessage, response);
    }
}
