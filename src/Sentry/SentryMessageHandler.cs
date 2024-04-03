using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Internal.Tracing;

namespace Sentry;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public abstract class SentryMessageHandler : DelegatingHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions? _options;
    private Func<HttpRequestMessage, string, string, RequestTracer> CreateRequestTracer { get; }

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

    /// <summary>
    /// Internal constructor for testing.
    /// </summary>
    /// <remarks>
    /// Potentially SDK users have inherited from this class and overriden either the <see cref="ProcessRequest"/> or
    /// <see cref="HandleResponse"/> methods. Those really should have been private internal but we can't change that
    /// without breaking changes. They've been marked as obsolete but, for the time being, this class defaults to using
    /// those obsolete overrides for tracing operations. This can be changed by setting <paramref name="useNewTracing"/>
    /// to true, which we do whenever creating an instance of this class internally. Eventually, in a future major
    /// release, this should be changed to be the default and the obsolete methods should be removed.
    /// </remarks>
    internal SentryMessageHandler(IHub? hub, SentryOptions? options, HttpMessageHandler? innerHandler = default,
        bool useNewTracing = false)
    {
        CreateRequestTracer = (useNewTracing)
            ? (request, method, url) => new AutoRequestTracer(this, request, method, url)
            : (request, method, url) => new SentryRequestTracer(this, request, method, url);
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
    [Obsolete("This method will be removed in future versions.")]
    protected internal abstract ISpan? ProcessRequest(HttpRequestMessage request, string method, string url);

    private protected abstract ITraceSpan? OnRequest(HttpRequestMessage request, string method, string url);

    /// <summary>
    /// Provides an opportunity for further processing of the span once a response is received.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/></param>
    /// <param name="span">The <see cref="ISpan"/> created in <see cref="ProcessRequest"/></param>
    /// <param name="method">The request method (e.g. "GET")</param>
    /// <param name="url">The request URL</param>
    [Obsolete("This method will be removed in future versions.")]
    protected internal abstract void HandleResponse(HttpResponseMessage response, ISpan? span, string method, string url);

    private protected abstract void OnResponse(HttpResponseMessage response, ITraceSpan? span, string method, string url);

    private abstract class RequestTracer
    {
        protected internal abstract void Finish(HttpResponseMessage response, string method, string url);
        protected internal abstract void Finish(Exception exception);
    }


#pragma warning disable CS0618 // Type or member is obsolete
    private class SentryRequestTracer : RequestTracer
    {
        private readonly SentryMessageHandler _handler;
        private readonly ISpan? _span;

        public SentryRequestTracer(SentryMessageHandler handler, HttpRequestMessage request, string method, string url)
        {
            _handler = handler;
            _span = _handler.ProcessRequest(request, method, url);
        }

        protected internal override void Finish(HttpResponseMessage response, string method, string url)
            => _handler.HandleResponse(response, _span, method, url);

        protected internal override void Finish(Exception exception) => _span?.Finish(exception);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    private class AutoRequestTracer : RequestTracer
    {
        private readonly SentryMessageHandler _handler;
        private readonly ITraceSpan? _span;

        public AutoRequestTracer(SentryMessageHandler handler, HttpRequestMessage request, string method, string url)
        {
            _handler = handler;
            _span = _handler.OnRequest(request, method, url);
        }

        protected internal override void Finish(HttpResponseMessage response, string method, string url)
            => _handler.OnResponse(response, _span, method, url);

        protected internal override void Finish(Exception exception) => _span?.Finish(exception);
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var method = request.Method.Method.ToUpperInvariant();
        var url = request.RequestUri?.ToString() ?? string.Empty;

        var requestTracer = CreateRequestTracer(request, method, url);
        try
        {
            PropagateTraceHeaders(request, url);
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            requestTracer.Finish(response, method, url);
            return response;
        }
        catch (Exception ex)
        {
            requestTracer.Finish(ex);
            throw;
        }
    }

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var method = request.Method.Method.ToUpperInvariant();
        var url = request.RequestUri?.ToString() ?? string.Empty;

        var requestTracer = CreateRequestTracer(request, method, url);
        try
        {
            PropagateTraceHeaders(request, url);
            var response = base.Send(request, cancellationToken);
            requestTracer.Finish(response, method, url);
            return response;
        }
        catch (Exception ex)
        {
            requestTracer?.Finish(ex);
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
