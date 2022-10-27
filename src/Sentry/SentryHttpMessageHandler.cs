using System.Net.Http;
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
    /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    public SentryHttpMessageHandler(IHub hub)
    {
        _hub = hub;
        _options = hub.GetSentryOptions();
    }

    internal SentryHttpMessageHandler(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    public SentryHttpMessageHandler(HttpMessageHandler innerHandler, IHub hub)
        : this(hub)
    {
        InnerHandler = innerHandler;
    }

    internal SentryHttpMessageHandler(HttpMessageHandler innerHandler, IHub hub, SentryOptions options)
        : this(hub, options)
    {
        InnerHandler = innerHandler;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    public SentryHttpMessageHandler(HttpMessageHandler innerHandler)
        : this(innerHandler, HubAdapter.Instance) { }

    /// <summary>
    /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    public SentryHttpMessageHandler()
        : this(HubAdapter.Instance) { }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Prevent null reference exception in the following call
        // in case the user didn't set an inner handler.
        InnerHandler ??= new HttpClientHandler();

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
