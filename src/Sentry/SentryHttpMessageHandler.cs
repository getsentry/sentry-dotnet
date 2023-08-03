using Sentry.Infrastructure;

namespace Sentry;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public class SentryHttpMessageHandler : SentryMessageHandler
{
    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    public SentryHttpMessageHandler()
        : base(default, default, default) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    public SentryHttpMessageHandler(HttpMessageHandler innerHandler)
        : base(default, default, innerHandler) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    public SentryHttpMessageHandler(IHub hub)
        : base(hub, default)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    /// <param name="hub">The Sentry hub.</param>
    public SentryHttpMessageHandler(HttpMessageHandler innerHandler, IHub hub)
        : base(hub, default, innerHandler)
    {
    }

    internal SentryHttpMessageHandler(IHub? hub, SentryOptions? options, HttpMessageHandler? innerHandler = default,
        ISentryFailedRequestHandler? failedRequestHandler = null)
        : base(hub, options, innerHandler, failedRequestHandler)
    {
    }

    /// <inheritdoc />
    protected internal override void OnProcessRequest(HttpRequestMessage request, ISpan? span, string method, string url)
    {
    }

    /// <inheritdoc />
    protected internal override Breadcrumb GetBreadcrumb(HttpResponseMessage response, ISpan? span, string method, string url)
        => new(
            SystemClock.Clock.GetUtcNow(),
            type: "http",
            category: "http",
            data: new Dictionary<string, string>
            {
                {"url", url},
                {"method", method},
                {"status_code", ((int) response.StatusCode).ToString()}
            }
        );

    /// <inheritdoc />
    protected internal override void OnBeforeFinishSpan(HttpResponseMessage response, ISpan span, string method, string url) =>
        SpanStatusConverter.FromHttpStatusCode(response.StatusCode);
}
