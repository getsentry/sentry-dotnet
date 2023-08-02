using Sentry.Extensibility;
using Sentry.Internal.Extensions;

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
    protected override ISpan DoStartChildSpan(ISpan parentSpan, HttpRequestMessage request, string method, string url)
    {
        return parentSpan.StartChild("http.client", $"{method} {url}");
    }

    /// <inheritdoc />
    protected override void DoAddBreadcrumb(IHub hub, HttpResponseMessage response, ISpan? span, string method,
        string url)
    {
        var breadcrumbData = new Dictionary<string, string>
        {
            {"url", url},
            {"method", method},
            {"status_code", ((int) response.StatusCode).ToString()}
        };
        hub.AddBreadcrumb(string.Empty, "http", "http", breadcrumbData);
    }

    /// <inheritdoc />
    protected override SpanStatus DetermineSpanStatus(HttpResponseMessage response) =>
        SpanStatusConverter.FromHttpStatusCode(response.StatusCode);
}
