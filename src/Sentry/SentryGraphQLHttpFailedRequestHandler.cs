using Sentry.Internal;
using Sentry.Internal.DataCollection;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry;

internal class SentryGraphQLHttpFailedRequestHandler : SentryFailedRequestHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    internal const string MechanismType = "GraphqlInstrumentation";
    private readonly SentryHttpFailedRequestHandler _httpFailedRequestHandler;

    internal SentryGraphQLHttpFailedRequestHandler(IHub hub, SentryOptions options)
        : base(hub, options)
    {
        _hub = hub;
        _options = options;
        _httpFailedRequestHandler = new SentryHttpFailedRequestHandler(hub, options);
    }

    protected internal override void DoEnsureSuccessfulResponse([NotNull] HttpRequestMessage request, [NotNull] HttpResponseMessage response)
    {
        JsonElement? json = null;
        try
        {
            json = GraphQLContentExtractor.ExtractResponseContentAsync(response, _options).Result;
            if (json is { } jsonElement)
            {
                if (jsonElement.TryGetProperty("errors", out var errorsElement))
                {
                    // We just show the first error... maybe there's a better way to do this when multiple errors exist.
                    // We should check what the Java code is doing.
                    var errorMessage = errorsElement[0].GetProperty("message").GetString() ?? "GraphQL Error";
                    throw new GraphQLHttpRequestException(errorMessage);
                }
            }
            // No GraphQL errors, but we still might have an HTTP error status
            _httpFailedRequestHandler.DoEnsureSuccessfulResponse(request, response);
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(MechanismType, "GraphQL Failed Request Handler", false);

            var @event = new SentryEvent(exception);
            var hint = new SentryHint(HintTypes.HttpResponseMessage, response);

            var dataCollection = _options.DataCollectionResolver;
            var queryParams = dataCollection.UrlQueryParams;
            var uri = request.RequestUri;
            var sentryRequest = new SentryRequest
            {
                Url = uri?.FilteredHttpRequestUrl(queryParams),
                QueryString = QueryStringFilter.Filter(uri?.Query, queryParams),
                Fragment = QueryStringFilter.Filter(uri?.HttpRequestFragment(), dataCollection.UrlFragmentWithLegacyPiiGate),
                Method = request.Method.Method.ToUpperInvariant(),
                Cookies = request.Headers.GetCookies(dataCollection.Cookies),
                ApiTarget = "graphql"
            };
            sentryRequest.AddHeaders(request.Headers, dataCollection.RequestHeadersWithLegacyPiiGate);

            var responseContext = new Response
            {
                StatusCode = (short)response.StatusCode,
                Cookies = response.Headers.GetSetCookies(dataCollection.Cookies),
#if NET5_0_OR_GREATER
                // Starting with .NET 5, the content and headers are guaranteed to not be null.
                BodySize = response.Content?.Headers.ContentLength,
#else
                BodySize = response.Content?.Headers?.ContentLength,
#endif
            };
            responseContext.AddHeaders(response.Headers, dataCollection.ResponseHeadersWithLegacyPiiGate);

            var requestContent = request.GetFused<GraphQLRequestContent>();
            if (dataCollection.OutgoingRequestBody)
            {
                sentryRequest.Data = requestContent?.FilterRequestContent(dataCollection.GraphQLDocument, dataCollection.GraphQLVariables);
            }
            if (dataCollection.IncomingResponseBody)
            {
                responseContext.Data = json;
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
            Hub.CaptureEvent(@event, hint: hint);
        }
    }
}
