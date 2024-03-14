using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public abstract class SentryMessageHandler : DelegatingHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions? _options;

    /// <summary>
    /// Constructs an instance of <see cref="SentryMessageHandler"/>.
    /// </summary>
    protected SentryMessageHandler()
        : this(default, default, default) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    protected SentryMessageHandler(HttpMessageHandler innerHandler)
        : this(default, default, innerHandler) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryMessageHandler"/>.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    protected SentryMessageHandler(IHub hub)
        : this(hub, default)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="SentryMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    /// <param name="hub">The Sentry hub.</param>
    protected SentryMessageHandler(HttpMessageHandler innerHandler, IHub hub)
        : this(hub, default, innerHandler)
    {
    }

    internal SentryMessageHandler(IHub? hub, SentryOptions? options, HttpMessageHandler? innerHandler = default)
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? _hub.GetSentryOptions();

        // Only assign the inner handler if it is supplied.  We can't assign null or it will throw.
        // We also cannot assign a default value here, or it will throw when used with HttpMessageHandlerBuilderFilter.
        if (innerHandler is not null)
        {
            InnerHandler = innerHandler;
        }
    }

    /// <summary>
    /// Starts a span for a request
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/></param>
    /// <param name="method">The request method (e.g. "GET")</param>
    /// <param name="url">The request URL</param>
    /// <returns>An <see cref="ISpan"/></returns>
    protected internal abstract ISpan? ProcessRequest(HttpRequestMessage request, string method, string url);

    /// <summary>
    /// Provides an opportunity for further processing of the span once a response is received.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/></param>
    /// <param name="span">The <see cref="ISpan"/> created in <see cref="ProcessRequest"/></param>
    /// <param name="method">The request method (e.g. "GET")</param>
    /// <param name="url">The request URL</param>
    protected internal abstract void HandleResponse(HttpResponseMessage response, ISpan? span, string method, string url);

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var method = request.Method.Method.ToUpperInvariant();
        var url = request.RequestUri?.ToString() ?? string.Empty;

        var span = ProcessRequest(request, method, url);
        try
        {
            PropagateTraceHeaders(request, url);
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
        var method = request.Method.Method.ToUpperInvariant();
        var url = request.RequestUri?.ToString() ?? string.Empty;

        var span = ProcessRequest(request, method, url);
        try
        {
            PropagateTraceHeaders(request, url);
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

    private void PropagateTraceHeaders(HttpRequestMessage request, string url)
    {
        // Assign a default inner handler for convenience the first time this is used.
        // We can't do this in a constructor, or it will throw when used with HttpMessageHandlerBuilderFilter.
        InnerHandler ??= new HttpClientHandler();

        if (_options?.TracePropagationTargets.ContainsMatch(url) is true or null)
        {
            AddSentryTraceHeader(request);
            AddBaggageHeader(request);
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
