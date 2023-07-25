using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public class SentryHttpMessageHandler : DelegatingHandler
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
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? _hub.GetSentryOptions();
        _failedRequestHandler = failedRequestHandler;

        // Only assign the inner handler if it is supplied.  We can't assign null or it will throw.
        // We also cannot assign a default value here, or it will throw when used with HttpMessageHandlerBuilderFilter.
        if (innerHandler is not null)
        {
            InnerHandler = innerHandler;
        }

        // Use the default failed request handler if none was supplied - but options is required.
        if (_failedRequestHandler == null && _options != null)
        {
            _failedRequestHandler = new SentryFailedRequestHandler(_hub, _options);
        }
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var (span, method, url) = ProcessRequest(request);

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            HandleResponse(response, span, method, url);
            return response;
        }
        catch (Exception ex)
        {
            span?.Finish(ex);
            throw;
        }
    }

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (span, method, url) = ProcessRequest(request);

        try
        {
            var response = base.Send(request, cancellationToken);
            HandleResponse(response, span, method, url);
            return response;
        }
        catch (Exception ex)
        {
            span?.Finish(ex);
            throw;
        }
    }
#endif

    private (ISpan? Span, string Method, string Url) ProcessRequest(HttpRequestMessage request)
    {
        // Assign a default inner handler for convenience the first time this is used.
        // We can't do this in a constructor, or it will throw when used with HttpMessageHandlerBuilderFilter.
        InnerHandler ??= new HttpClientHandler();

        var method = request.Method.Method.ToUpperInvariant();
        var url = request.RequestUri?.ToString() ?? string.Empty;

        if (_options?.TracePropagationTargets.ContainsMatch(url) is true or null)
        {
            AddSentryTraceHeader(request);
            AddBaggageHeader(request);
        }

        // Start a span that tracks this request
        // (may be null if transaction is not set on the scope)
        // e.g. "GET https://example.com"
        var span = _hub.GetSpan()?.StartChild("http.client", $"{method} {url}");

        return (span, method, url);
    }

    private void HandleResponse(HttpResponseMessage response, ISpan? span, string method, string url)
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
        var status = SpanStatusConverter.FromHttpStatusCode(response.StatusCode);
        span?.Finish(status);
    }

    private void AddSentryTraceHeader(HttpRequestMessage request)
    {
        // Set trace header if it hasn't already been set
        if (!request.Headers.Contains(SentryTraceHeader.HttpHeaderName) && _hub.GetTraceParent() is { } traceHeader)
        {
            request.Headers.Add(SentryTraceHeader.HttpHeaderName, traceHeader.ToString());
        }
    }

    private void AddBaggageHeader(HttpRequestMessage request)
    {
        var baggage = _hub.GetBaggage();
        if (baggage is null)
        {
            return;
        }

        if (request.Headers.TryGetValues(BaggageHeader.HttpHeaderName, out var baggageHeaders))
        {
            var headers = baggageHeaders.ToList();
            if (headers.Any(h => h.StartsWith(BaggageHeader.SentryKeyPrefix)))
            {
                // The Sentry headers have already been added to this request.  Do nothing.
                return;
            }

            // Merge existing baggage headers with ours.
            var allBaggage = headers
                .Select(s => BaggageHeader.TryParse(s)).ExceptNulls()
                .Append(baggage);
            baggage = BaggageHeader.Merge(allBaggage);

            // Remove the existing header so we can replace it with the merged one.
            request.Headers.Remove(BaggageHeader.HttpHeaderName);
        }

        // Set the baggage header
        request.Headers.Add(BaggageHeader.HttpHeaderName, baggage.ToString());
    }
}
