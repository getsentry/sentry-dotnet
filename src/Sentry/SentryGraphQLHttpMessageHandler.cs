using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.OpenTelemetry;

namespace Sentry;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public class SentryGraphQLHttpMessageHandler : SentryMessageHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions? _options;
    private readonly ISentryFailedRequestHandler? _failedRequestHandler;

    /// <summary>
    /// Constructs an instance of <see cref="SentryGraphQLHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    /// <param name="hub">The Sentry hub.</param>
    public SentryGraphQLHttpMessageHandler(HttpMessageHandler? innerHandler = default, IHub? hub = default)
        : this(hub, default, innerHandler)
    {
    }

    internal SentryGraphQLHttpMessageHandler(IHub? hub, SentryOptions? options,
        HttpMessageHandler? innerHandler = default,
        ISentryFailedRequestHandler? failedRequestHandler = null)
    : base(hub, options, innerHandler)
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? _hub.GetSentryOptions();
        _failedRequestHandler = failedRequestHandler;
        if (_options != null)
        {
            _failedRequestHandler ??= new SentryGraphQLHttpFailedRequestHandler(_hub, _options);
        }
    }

    /// <inheritdoc />
    protected internal override ISpan? ProcessRequest(HttpRequestMessage request, string method, string url)
    {
        var content = GraphQLContentExtractor.ExtractRequestContentAsync(request, _options).Result;
        if (content is not { } graphQlRequestContent)
        {
            _options?.LogDebug("Unable to process non GraphQL request content");
            return null;
        }
        request.SetFused(graphQlRequestContent);

        // Start a span that tracks this request
        // (may be null if transaction is not set on the scope)
        var span = _hub.GetSpan()?.StartChild(
            "http.client",
            $"{method} {url}" // e.g. "GET https://example.com"
        );
        span?.SetData(OtelSemanticConventions.AttributeHttpRequestMethod, method);
        return span;
    }

    /// <inheritdoc />
    protected internal override void HandleResponse(HttpResponseMessage response, ISpan? span, string method, string url)
    {
        var graphqlInfo = response.RequestMessage?.GetFused<GraphQLRequestContent>();
        var breadcrumbData = new Dictionary<string, string>
        {
            {"url", url},
            {"method", method},
            {"status_code", ((int) response.StatusCode).ToString()}
        };
        AddIfExists(breadcrumbData, "request_body_size", response.RequestMessage?.Content?.Headers.ContentLength?.ToString());
#if NET5_0_OR_GREATER
        // Starting with .NET 5, the content and headers are guaranteed to not be null.
        AddIfExists(breadcrumbData, "response_body_size", response.Content.Headers.ContentLength?.ToString());
#else
        AddIfExists(breadcrumbData, "response_body_size", response.Content?.Headers.ContentLength?.ToString());
#endif
        AddIfExists(breadcrumbData, "operation_name", graphqlInfo?.OperationName); // The GraphQL operation name
        AddIfExists(breadcrumbData, "operation_type", graphqlInfo?.OperationType); // i.e. `query`, `mutation`, `subscription`
        _hub.AddBreadcrumb(
            string.Empty,
            graphqlInfo?.OperationType ?? "graphql.operation",
            "graphql",
            breadcrumbData
            );

        // Create events for failed requests
        _failedRequestHandler?.HandleResponse(response);

        // This will handle unsuccessful status codes as well
        if (span is not null)
        {
            span.SetData(OtelSemanticConventions.AttributeHttpResponseStatusCode, (int)response.StatusCode);
            span.Description = GetSpanDescriptionOrDefault(graphqlInfo, response.StatusCode) ?? span.Description;
            // TODO: See how we can determine the span status for a GraphQL request...
            var status = SpanStatusConverter.FromHttpStatusCode(response.StatusCode);  // TODO: Don't do this if the span is errored
            span.Finish(status);
        }
    }

    private string? GetSpanDescriptionOrDefault(GraphQLRequestContent? graphqlInfo, HttpStatusCode statusCode) =>
        string.Join(" ",
            graphqlInfo?.OperationNameOrFallback(),
            graphqlInfo?.OperationTypeOrFallback(),
            ((int)statusCode).ToString()
        );

    private void AddIfExists(Dictionary<string, string> breadcrumbData, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            breadcrumbData[key] = value;
        }
    }
}
