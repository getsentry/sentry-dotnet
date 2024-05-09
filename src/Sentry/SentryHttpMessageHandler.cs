using Sentry.Extensibility;
using Sentry.Internal.OpenTelemetry;

namespace Sentry;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public class SentryHttpMessageHandler : SentryMessageHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions? _options;
    private readonly ISentryFailedRequestHandler? _failedRequestHandler;

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    public SentryHttpMessageHandler()
        : this(default, default, default) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    public SentryHttpMessageHandler(HttpMessageHandler innerHandler)
        : this(default, default, innerHandler) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    public SentryHttpMessageHandler(IHub hub)
        : this(hub, default)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    /// <param name="hub">The Sentry hub.</param>
    public SentryHttpMessageHandler(HttpMessageHandler innerHandler, IHub hub)
        : this(hub, default, innerHandler)
    {
    }

    internal SentryHttpMessageHandler(IHub? hub, SentryOptions? options, HttpMessageHandler? innerHandler = default, ISentryFailedRequestHandler? failedRequestHandler = null)
        : base(hub, options, innerHandler)
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? _hub.GetSentryOptions();
        _failedRequestHandler = failedRequestHandler;

        // Use the default failed request handler if none was supplied - but options is required.
        if (_failedRequestHandler == null && _options != null)
        {
            _failedRequestHandler = new SentryHttpFailedRequestHandler(_hub, _options);
        }
    }

    /// <inheritdoc />
    protected internal override ISpan? ProcessRequest(HttpRequestMessage request, string method, string url)
    {
        // Start a span that tracks this request
        // (may be null if transaction is not set on the scope)
        var span = _hub.GetSpan()?.StartChild(
            "http.client",
            $"{method} {url}" // e.g. "GET https://example.com"
            );
        span?.SetExtra(OtelSemanticConventions.AttributeHttpRequestMethod, method);
        if (!string.IsNullOrWhiteSpace(request.RequestUri?.Host))
        {
            span?.SetExtra(OtelSemanticConventions.AttributeServerAddress, request.RequestUri!.Host);
        }
        return span;
    }

    /// <inheritdoc />
    protected internal override void HandleResponse(HttpResponseMessage response, ISpan? span, string method, string url)
    {
        var breadcrumbData = new Dictionary<string, string>
        {
            {"url", url},
            {"method", method},
            {"status_code", ((int) response.StatusCode).ToString()}
        };
        _hub.AddBreadcrumb(string.Empty, "http", "http", breadcrumbData);

        // Create events for failed requests
        _failedRequestHandler?.HandleResponse(response);

        // This will handle unsuccessful status codes as well
        if (span is not null)
        {
            span.SetExtra(OtelSemanticConventions.AttributeHttpResponseStatusCode, (int)response.StatusCode);
            var status = SpanStatusConverter.FromHttpStatusCode(response.StatusCode);
            span.Finish(status);
        }
    }
}
