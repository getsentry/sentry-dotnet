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

    internal SentryHttpMessageHandler(IHub? hub, SentryOptions? options, HttpMessageHandler? innerHandler = default)
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? _hub.GetSentryOptions();
        InnerHandler = innerHandler ?? new HttpClientHandler();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestMethod = request.Method.Method.ToUpperInvariant();
        var url = request.RequestUri?.ToString() ?? string.Empty;

        if (_options?.TracePropagationTargets.ShouldPropagateTrace(url) is true or null)
        {
            AddSentryTraceHeader(request);
            AddBaggageHeader(request);
        }

        // Start a span that tracks this request
        // (may be null if transaction is not set on the scope)
        var span = _hub.GetSpan()?.StartChild(
            "http.client",
            // e.g. "GET https://example.com"
            $"{requestMethod} {url}");

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var breadcrumbData = new Dictionary<string, string>
            {
                { "url", url },
                { "method", requestMethod },
                { "status_code", ((int)response.StatusCode).ToString() }
            };
            _hub.AddBreadcrumb(string.Empty, "http", "http", breadcrumbData);

            // This will handle unsuccessful status codes as well
            span?.Finish(SpanStatusConverter.FromHttpStatusCode(response.StatusCode));

            return response;
        }
        catch (Exception ex)
        {
            span?.Finish(ex);
            throw;
        }
    }

    private void AddSentryTraceHeader(HttpRequestMessage request)
    {
        // Set trace header if it hasn't already been set
        if (!request.Headers.Contains(SentryTraceHeader.HttpHeaderName) && _hub.GetTraceHeader() is { } traceHeader)
        {
            request.Headers.Add(SentryTraceHeader.HttpHeaderName, traceHeader.ToString());
        }
    }

    private void AddBaggageHeader(HttpRequestMessage request)
    {
        var transaction = _hub.GetSpan();
        if (transaction is not TransactionTracer {DynamicSamplingContext: {IsEmpty: false} dsc})
        {
            return;
        }

        var baggage = dsc.ToBaggageHeader();

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
