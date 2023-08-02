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
    private readonly ISentryFailedRequestHandler? _failedRequestHandler;

    internal SentryMessageHandler(
        IHub? hub,
        SentryOptions? options,
        HttpMessageHandler? innerHandler = default,
        ISentryFailedRequestHandler? failedRequestHandler = null,
        Func<IHub, SentryOptions, ISentryFailedRequestHandler>? failedRequestHandlerResolver = null
        )
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
            var resolver = failedRequestHandlerResolver ?? SentryHttpFailedRequestHandler.Create;
            _failedRequestHandler = resolver(_hub, _options);
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
        var span = (_hub.GetSpan() is { } parentSpan)
            ? DoStartChildSpan(parentSpan, request, method, url)
            : null;

        return (span, method, url);
    }

    /// <summary>
    /// Creates a <see cref="ISpan"/> for each message being handled.
    /// </summary>
    /// <param name="parentSpan">The parent <see cref="ISpan"/></param>
    /// <param name="request">The <see cref="HttpRequestMessage"/></param>
    /// <param name="method">The request method (in upper case)</param>
    /// <param name="url">The request url (as a string)</param>
    /// <returns>An <see cref="ISpan"/> child of the parentSpan</returns>
    protected abstract ISpan DoStartChildSpan(ISpan parentSpan, HttpRequestMessage request, string method, string url);

    /// <summary>
    /// Adds a <see cref="Breadcrumb"/> for a each handled message.
    /// </summary>
    /// <param name="hub">A <see cref="IHub"/> that can be used to create the breadcrumb</param>
    /// <param name="response">The <see cref="HttpResponseMessage"/></param>
    /// <param name="span">The <see cref="ISpan"/> representing the message</param>
    /// <param name="method">The request method (in upper case)</param>
    /// <param name="url">The request url (as a string)</param>
    /// <returns></returns>
    protected abstract void DoAddBreadcrumb(IHub hub, HttpResponseMessage response, ISpan? span, string method, string url);

    /// <summary>
    /// Determine the status that should be used for a handled message.
    /// </summary>
    /// <param name="response">A <see cref="HttpResponseMessage"/></param>
    /// <returns>The <see cref="SpanStatus"/> to use when recording Spans for this message handler</returns>
    protected abstract SpanStatus DetermineSpanStatus(HttpResponseMessage response);

    private void HandleResponse(HttpResponseMessage response, ISpan? span, string method, string url)
    {
        DoAddBreadcrumb(_hub, response, span, method, url);

        // Create events for failed requests
        _failedRequestHandler?.HandleResponse(response);

        // This will handle unsuccessful status codes as well
        var status = DetermineSpanStatus(response);
        span?.Finish(status);
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
