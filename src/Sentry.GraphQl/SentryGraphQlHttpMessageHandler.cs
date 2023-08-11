namespace Sentry.GraphQl;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public class SentryGraphQlHttpMessageHandler : SentryMessageHandler
{
    private readonly GraphQlContentExtractor _extractor;
    private readonly IHub _hub;
    private readonly SentryOptions? _options;
    private readonly ISentryFailedRequestHandler? _failedRequestHandler;

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    /// <param name="hub">The Sentry hub.</param>
    public SentryGraphQlHttpMessageHandler(HttpMessageHandler? innerHandler = default, IHub? hub = default)
        : this(hub, default, innerHandler)
    {
    }

    internal SentryGraphQlHttpMessageHandler(IHub? hub, SentryOptions? options,
        HttpMessageHandler? innerHandler = default,
        ISentryFailedRequestHandler? failedRequestHandler = null)
    : base(hub, options, innerHandler)
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? _hub.GetSentryOptions();
        _extractor = new GraphQlContentExtractor(options);
        _failedRequestHandler = failedRequestHandler;
        if (_options != null)
        {
            _failedRequestHandler ??= new SentryGraphQlHttpFailedRequestHandler(_hub, _options, _extractor);
        }
    }

    /// <inheritdoc />
    protected internal override ISpan? ProcessRequest(HttpRequestMessage request, string method, string url)
    {
        var content = _extractor.ExtractRequestContentAsync(request).Result;
        if (content is not { } graphQlRequestContent)
        {
            _options?.LogDebug("Unable to process non GraphQL request content");
            return null;
        }
        request.SetFused(graphQlRequestContent);

        // Start a span that tracks this request
        // (may be null if transaction is not set on the scope)
        return _hub.GetSpan()?.StartChild(
            "http.client",
            $"{method} {url}" // e.g. "GET https://example.com"
        );
    }

    /// <inheritdoc />
    protected internal override void HandleResponse(HttpResponseMessage response, ISpan? span, string method, string url)
    {
        var graphqlInfo = response.RequestMessage?.GetFused<GraphQlRequestContent>();
        var breadcrumbData = new Dictionary<string, string>
        {
            {"url", url},
            {"method", method},
            {"status_code", ((int) response.StatusCode).ToString()}
        };
        AddIfExists(breadcrumbData, "request_body_size", response.RequestMessage?.Content?.Headers.ContentLength?.ToString());
        AddIfExists(breadcrumbData, "response_body_size", response.Content.Headers.ContentLength?.ToString());
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
            // TODO: See how we can determine the span status for a GraphQL request...
            span.Status = SpanStatusConverter.FromHttpStatusCode(response.StatusCode); // TODO: Don't do this if the span is errored
            span.Description = GetSpanDescriptionOrDefault(graphqlInfo, response.StatusCode) ?? span.Description;
            span.Finish();
        }
    }

    private string? GetSpanDescriptionOrDefault(GraphQlRequestContent? graphqlInfo, HttpStatusCode statusCode) =>
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
