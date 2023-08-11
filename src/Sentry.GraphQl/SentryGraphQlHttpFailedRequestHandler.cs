namespace Sentry.GraphQl;

internal class SentryGraphQlHttpFailedRequestHandler : SentryFailedRequestHandler
{
    private readonly SentryOptions _options;
    internal const string MechanismType = "SentryGraphQLHttpFailedRequestHandler";

    internal SentryGraphQlHttpFailedRequestHandler(IHub hub, SentryOptions options)
        : base(hub, options)
    {
        _options = options;
    }

    private static readonly Regex ErrorsRegex = new ("(?i)\"errors\"\\s*:\\s*\\[", RegexOptions.Compiled);

    protected internal override void DoEnsureSuccessfulResponse([NotNull]HttpRequestMessage request, [NotNull]HttpResponseMessage response)
    {
        try
        {
            var json = response.Content?.ReadAsJson();
            if (json is { } jsonElement)
            {
                if (jsonElement.TryGetProperty("errors", out var errorsElement))
                {
                    throw new SentryGraphQlHttpFailedRequestException("GraphQL Error");
                }
            }
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(MechanismType);

            var @event = new SentryEvent(exception);
            var hint = new Hint(HintTypes.HttpResponseMessage, response);

            var requestContent = request.GetFused<GraphQlRequestContent>();
            var sentryRequest = new Request
            {
                QueryString = request.RequestUri?.Query,
                Method = request.MethodString(),
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

            if (!_options.SendDefaultPii)
            {
                sentryRequest.Url = request.RequestUri?.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            else
            {
                sentryRequest.Url = request.RequestUri?.AbsoluteUri;
                sentryRequest.Cookies = request.Headers.GetCookies();
                sentryRequest.AddHeaders(request.Headers);
                responseContext.Cookies = response.Headers.GetCookies();
                responseContext.AddHeaders(response.Headers);
            }

            @event.Request = sentryRequest;
            @event.Contexts[Response.Type] = responseContext;

            Hub.CaptureEvent(@event, hint);
        }
    }
}
