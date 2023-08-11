namespace Sentry.GraphQl;

internal class SentryGraphQlHttpFailedRequestHandler : SentryFailedRequestHandler
{
    private readonly SentryOptions _options;
    private readonly GraphQlContentExtractor _extractor;
    internal const string MechanismType = "SentryGraphQLHttpFailedRequestHandler";

    internal SentryGraphQlHttpFailedRequestHandler(IHub hub, SentryOptions options, GraphQlContentExtractor? extractor = null)
        : base(hub, options)
    {
        _options = options;
        _extractor = extractor ?? new GraphQlContentExtractor(options);
    }

    private static readonly Regex ErrorsRegex = new ("(?i)\"errors\"\\s*:\\s*\\[", RegexOptions.Compiled);

    protected internal override void DoEnsureSuccessfulResponse([NotNull]HttpRequestMessage request, [NotNull]HttpResponseMessage response)
    {
        JsonElement? json = null;
        try
        {
            json = _extractor.ExtractResponseContentAsync(response).Result;
            if (json is not { } jsonElement)
            {
                return;
            }
            if (jsonElement.TryGetProperty("errors", out var errorsElement))
            {
                throw new SentryGraphQlHttpFailedRequestException("GraphQL Error");
            }
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(MechanismType);

            var @event = new SentryEvent(exception);
            var hint = new Hint(HintTypes.HttpResponseMessage, response);

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

            var requestContent = request.GetFused<GraphQlRequestContent>();
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
