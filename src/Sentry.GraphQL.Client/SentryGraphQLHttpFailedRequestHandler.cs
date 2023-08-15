namespace Sentry.GraphQL.Client;

internal class SentryGraphQLHttpFailedRequestHandler : SentryFailedRequestHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    internal const string MechanismType = "SentryGraphQLHttpFailedRequestHandler";
    private readonly SentryHttpFailedRequestHandler _httpFailedRequestHandler;

    internal SentryGraphQLHttpFailedRequestHandler(IHub hub, SentryOptions options)
        : base(hub, options)
    {
        _hub = hub;
        _options = options;
        _httpFailedRequestHandler = new SentryHttpFailedRequestHandler(hub, options);
    }

    protected internal override void DoEnsureSuccessfulResponse([NotNull]HttpRequestMessage request, [NotNull]HttpResponseMessage response)
    {
        JsonElement? json = null;
        try
        {
            json = GraphQLContentExtractor.ExtractResponseContentAsync(response, _options).Result;
            if (json is { } jsonElement)
            {
                if (jsonElement.TryGetProperty("errors", out var errorsElement))
                {
                    throw new SentryGraphQLHttpFailedRequestException("GraphQL Error");
                }
            }
            // No GraphQL errors, but we still might have an HTTP error status
            _httpFailedRequestHandler.DoEnsureSuccessfulResponse(request, response);
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(MechanismType);

            var @event = new SentryEvent(exception);
            var hint = new Hint(HintTypes.HttpResponseMessage, response);

            var sentryRequest = new Request
            {
                QueryString = request.RequestUri?.Query,
                Method = request.Method.Method.ToUpperInvariant(),
                ApiTarget = "graphql"
            };

            var responseContext = new Response
            {
                StatusCode = (short)response.StatusCode,
#if NET5_0_OR_GREATER
                // Starting with .NET 5, the content and headers are guaranteed to not be null.
                BodySize = response.Content?.Headers.ContentLength,
#else
                BodySize = response.Content?.Headers?.ContentLength,
#endif
            };

            var requestContent = request.GetFused<GraphQLRequestContent>();
            if (!_options.SendDefaultPii)
            {
                sentryRequest.Url = request.RequestUri?.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            else
            {
                sentryRequest.Cookies = request.Headers.GetCookies();
                sentryRequest.Data = requestContent?.Query;
                sentryRequest.Url = request.RequestUri?.AbsoluteUri;
                sentryRequest.AddHeaders(request.Headers);
                responseContext.Cookies = response.Headers.GetCookies();
                responseContext.Data = json;
                responseContext.AddHeaders(response.Headers);
            }

            @event.Request = sentryRequest;
            @event.Contexts[Response.Type] = responseContext;
            if (requestContent is not null)
            {
                @event.Fingerprint = new[]
                {
                    requestContent.OperationNameOrFallback(),
                    requestContent.OperationTypeOrFallback(),
                    ((int)response.StatusCode).ToString()
                };
            }
            Hub.CaptureEvent(@event, hint);
        }
    }
}
